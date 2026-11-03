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

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Items).ThenInclude(i => i.Producto)
            .Include(c => c.Sesiones)
            .Include(c => c.Suplementos)
            .FirstOrDefaultAsync(c => c.Id == id);

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
                Nombre = i.Producto != null ? i.Producto.Nombre : "Eliminado",
                i.Cantidad,
                i.PrecioVentaUSD,
                i.SubtotalUSD
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

    public record ItemRequest(Guid? ProductoId, int Cantidad, decimal PrecioVentaUSD);
    public record CrearContratoRequest(
        Guid ClienteId, 
        DateTime? FechaSesion, 
        string Locacion, 
        string Estado,
        List<ItemRequest> Items);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearContratoRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(request.ClienteId);
        if (cliente == null) return BadRequest("El cliente no existe.");

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

        foreach (var item in request.Items)
        {
            var nuevoItem = new ContractItem
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioVentaUSD = item.PrecioVentaUSD,
                SubtotalUSD = item.Cantidad * item.PrecioVentaUSD
            };
            contrato.Items.Add(nuevoItem);
            contrato.TotalUSD += nuevoItem.SubtotalUSD;
        }

        if (request.FechaSesion.HasValue)
        {
            contrato.Sesiones.Add(new Sesion
            {
                FechaInicio = request.FechaSesion.Value.ToUniversalTime(),
                FechaFin = request.FechaSesion.Value.AddHours(2).ToUniversalTime(),
                Locacion = request.Locacion ?? "Por definir"
            });
        }

        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contrato creado", numero = contrato.NumeroContrato, id = contrato.Id });
    }

    public record ActualizarEstadoRequest(string NuevoEstado);

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(Guid id, ActualizarEstadoRequest request)
    {
        var contrato = await _context.Contratos.FindAsync(id);
        if (contrato == null) return NotFound("Contrato no encontrado.");

        var estadosValidos = new[] { "Borrador", "Firmado", "Pagado", "Cancelado" };
        if (!estadosValidos.Contains(request.NuevoEstado))
            return BadRequest("Estado no válido.");

        contrato.Estado = request.NuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Contrato actualizado a {request.NuevoEstado}" });
    }
}