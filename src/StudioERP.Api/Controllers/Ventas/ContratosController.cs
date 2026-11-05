using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Ventas;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ContratosController : ControllerBase
{
    private readonly StudioDbContext _context;

    public ContratosController(StudioDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var contratos = await _context.Contratos
            .Include(c => c.Cliente)
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.NumeroContrato,
                c.Estado,
                c.TotalUSD,
                Cliente = $"{c.Cliente.Nombre} {c.Cliente.Apellidos}",
                FechaCreacion = c.CreatedAt
            })
            .ToListAsync();

        return Ok(contratos);
    }

    [Authorize(Roles = "Admin,Fotógrafo,Cajero")]
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Items).ThenInclude(i => i.Producto)
            .Include(c => c.Items).ThenInclude(i => i.FichaCostoProducto)
            .Include(c => c.Items).ThenInclude(i => i.Paquete)
            .Include(c => c.Items).ThenInclude(i => i.Promocion)
            .Include(c => c.Sesiones)
            .Include(c => c.Suplementos)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (contrato == null) return NotFound("Contrato no encontrado.");

        return Ok(new
        {
            contrato.Id,
            contrato.NumeroContrato,
            contrato.Estado,
            contrato.TotalUSD,
            FechaCreacion = contrato.CreatedAt,
            Cliente = $"{contrato.Cliente.Nombre} {contrato.Cliente.Apellidos}",
            Items = contrato.Items.Select(i => new
            {
                i.Id,
                ProductoId = i.ProductoId,
                PaqueteId = i.PaqueteId,
                FichaCostoProductoId = i.FichaCostoProductoId,
                Nombre = i.Producto?.Nombre ?? i.Paquete?.Nombre ?? "Eliminado",
                Variante = i.FichaCostoProducto != null ? i.FichaCostoProducto.Variante : null,
                Contexto = i.FichaCostoProducto != null ? i.FichaCostoProducto.Contexto : null,
                i.Cantidad,
                i.PrecioVentaUSD,
                i.SubtotalUSD,
                Promocion = i.Promocion?.Nombre
            }).ToList(),
            Sesiones = contrato.Sesiones.Select(s => new
            {
                s.Id,
                s.FechaInicio,
                s.Locacion
            }).ToList(),
            Suplementos = contrato.Suplementos.Select(s => new
            {
                s.Id,
                s.NumeroSuplemento,
                s.Estado,
                s.TotalUSD
            }).ToList()
        });
    }

    public record ItemRequest(
        Guid? ProductoId,
        int Cantidad,
        decimal PrecioVentaUSD,
        Guid? PaqueteId = null,
        Guid? FichaCostoProductoId = null);
    public record CrearContratoRequest(
        Guid ClienteId, 
        DateTime? FechaSesion, 
        string Locacion, 
        string Estado,
        List<ItemRequest> Items,
        Guid? PersonalId = null,
        decimal? ComisionPactadaUSD = null);

    [Authorize(Roles = "Admin,Fotógrafo")]
    [HttpPost]
    public async Task<IActionResult> Crear(CrearContratoRequest request)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(item => item.Id == request.ClienteId && !item.IsDeleted);
        if (cliente == null) return BadRequest("El cliente no existe o está inactivo.");
        if (request.Items == null || request.Items.Any(item =>
                item.Cantidad <= 0 ||
                item.ProductoId.HasValue == item.PaqueteId.HasValue ||
                (item.FichaCostoProductoId.HasValue && !item.ProductoId.HasValue)))
            return BadRequest("Cada ítem debe ser un producto o paquete activo y tener una cantidad positiva.");
        if (request.PersonalId.HasValue && !request.FechaSesion.HasValue)
            return BadRequest("Para asignar personal debe indicar la fecha de la sesión.");
        if (request.ComisionPactadaUSD < 0)
            return BadRequest("La comisión pactada no puede ser negativa.");

        Staff? personal = null;
        if (request.PersonalId.HasValue)
        {
            personal = await _context.Staffs
                .Include(staff => staff.Usuario)
                .Include(staff => staff.BloqueosAgenda)
                .FirstOrDefaultAsync(staff => staff.Id == request.PersonalId &&
                                              !staff.IsDeleted &&
                                              !staff.Usuario.IsDeleted &&
                                              staff.Usuario.Activo);
            if (personal == null)
                return BadRequest("El personal seleccionado no existe o está inactivo.");
        }

        var inicioSesion = request.FechaSesion?.ToUniversalTime();
        var finSesion = inicioSesion?.AddHours(2);
        if (personal != null && inicioSesion.HasValue && finSesion.HasValue)
        {
            if (personal.BloqueosAgenda.Any(b => !b.IsDeleted &&
                                                  b.FechaInicio <= finSesion.Value &&
                                                  b.FechaFin >= inicioSesion.Value))
                return BadRequest("El personal seleccionado no está disponible en esa fecha.");

            var tieneOtraSesion = await _context.SessionStaffs
                .AnyAsync(asignacion => asignacion.StaffId == personal.Id &&
                                        !asignacion.IsDeleted &&
                                        !asignacion.Sesion.IsDeleted &&
                                        asignacion.Sesion.FechaInicio < finSesion.Value &&
                                        asignacion.Sesion.FechaFin > inicioSesion.Value);
            if (tieneOtraSesion)
                return BadRequest("El personal seleccionado ya tiene una sesión en ese horario.");
        }

        var year = DateTime.UtcNow.Year;
        var secuencia = await _context.DocumentSequences
            .FirstOrDefaultAsync(d => d.DocumentType == "CONTRATO" && d.Year == year);
        
        if (secuencia == null)
        {
            secuencia = new DocumentSequence { DocumentType = "CONTRATO", Year = year, LastNumber = 0 };
            _context.DocumentSequences.Add(secuencia);
        }

        secuencia.LastNumber++;
        var numeroContrato = $"CTR-{year}-{secuencia.LastNumber:D4}";

        var estadosValidos = new[] { "Borrador", "Firmado", "Pagado" };
        var estadoFinal = string.IsNullOrEmpty(request.Estado) || !estadosValidos.Contains(request.Estado) 
            ? "Borrador" 
            : request.Estado;

        var contrato = new Contrato
        {
            ClienteId = request.ClienteId,
            NumeroContrato = numeroContrato,
            Estado = estadoFinal,
            TotalUSD = 0
        };

        var fechaActual = DateTime.UtcNow;
        foreach (var item in request.Items)
        {
            decimal precioBase;
            Promocion? promocion;
            if (item.ProductoId.HasValue)
            {
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Id == item.ProductoId && !p.IsDeleted);
                if (producto == null)
                    return BadRequest("Uno de los productos no existe o está inactivo.");
                precioBase = producto.PrecioVentaBaseUSD;
                if (item.FichaCostoProductoId.HasValue)
                {
                    var fichaCosto = await _context.FichasCostoProductos
                        .FirstOrDefaultAsync(ficha =>
                            ficha.Id == item.FichaCostoProductoId &&
                            ficha.ProductoId == producto.Id &&
                            !ficha.IsDeleted);
                    if (fichaCosto == null)
                        return BadRequest("La ficha de costo seleccionada no pertenece a un producto activo.");
                    precioBase = fichaCosto.PrecioVentaUSD;
                }
                promocion = await _context.Promociones.FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.ProductoId == item.ProductoId &&
                    p.FechaInicio <= fechaActual &&
                    p.FechaFin >= fechaActual);
            }
            else
            {
                var paquete = await _context.Paquetes
                    .FirstOrDefaultAsync(p => p.Id == item.PaqueteId && !p.IsDeleted);
                if (paquete == null)
                    return BadRequest("Uno de los paquetes no existe o está inactivo.");
                precioBase = paquete.PrecioVentaFinalUSD;
                promocion = await _context.Promociones.FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.PaqueteId == item.PaqueteId &&
                    p.FechaInicio <= fechaActual &&
                    p.FechaFin >= fechaActual);
            }

            var precioFinal = promocion?.TipoDescuento switch
            {
                "FijoUSD" => Math.Max(0, precioBase - promocion.ValorDescuento),
                "Porcentaje" => precioBase * (1 - promocion.ValorDescuento / 100),
                "TrabajoGratis" => 0,
                _ => precioBase
            };
            var nuevoItem = new ContractItem
            {
                ProductoId = item.ProductoId,
                PaqueteId = item.PaqueteId,
                FichaCostoProductoId = item.FichaCostoProductoId,
                PromocionId = promocion?.Id,
                Cantidad = item.Cantidad,
                PrecioVentaUSD = precioFinal,
                SubtotalUSD = item.Cantidad * precioFinal
            };
            contrato.Items.Add(nuevoItem);
            contrato.TotalUSD += nuevoItem.SubtotalUSD;
        }

        if (inicioSesion.HasValue && finSesion.HasValue)
        {
            var sesion = new Sesion
            {
                FechaInicio = inicioSesion.Value,
                FechaFin = finSesion.Value,
                Locacion = request.Locacion ?? "Por definir"
            };
            if (personal != null)
            {
                sesion.PersonalAsignado.Add(new SessionStaff
                {
                    StaffId = personal.Id,
                    ComisionPactadaUSD = request.ComisionPactadaUSD ?? 0
                });
            }
            contrato.Sesiones.Add(sesion);
        }

        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contrato creado", numero = contrato.NumeroContrato, id = contrato.Id });
    }

    public record ActualizarEstadoRequest(string NuevoEstado);

    [Authorize(Roles = "Admin,Fotógrafo")]
    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(Guid id, ActualizarEstadoRequest request)
    {
        if (request.NuevoEstado == "Cancelado" && !User.IsInRole("Admin"))
            return Forbid();

        var contrato = await _context.Contratos
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (contrato == null) return NotFound("Contrato no encontrado.");

        var estadosValidos = new[] { "Borrador", "Firmado", "Pagado", "Cancelado" };
        if (!estadosValidos.Contains(request.NuevoEstado))
            return BadRequest("Estado no válido.");

        contrato.Estado = request.NuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Contrato actualizado a {request.NuevoEstado}" });
    }
}