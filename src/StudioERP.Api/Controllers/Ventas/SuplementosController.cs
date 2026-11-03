using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Ventas;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class SuplementosController : ControllerBase
{
    private readonly StudioDbContext _context;

    public SuplementosController(StudioDbContext context)
    {
        _context = context;
    }

    [HttpGet("contrato/{contratoId}")]
    public async Task<IActionResult> ObtenerPorContrato(Guid contratoId)
    {
        var suplementos = await _context.Suplementos
            .Where(s => s.ContratoId == contratoId && !s.IsDeleted)
            .Include(s => s.Items).ThenInclude(i => i.Producto)
            .Select(s => new
            {
                s.Id,
                s.NumeroSuplemento,
                s.Estado,
                s.TotalUSD,
                s.Motivo,
                FechaCreacion = s.CreatedAt,
                Items = s.Items.Select(i => new
                {
                    i.Id,
                    Producto = i.Producto != null ? i.Producto.Nombre : "Eliminado",
                    i.Cantidad,
                    i.PrecioVentaUSD,
                    i.SubtotalUSD
                }).ToList()
            })
            .ToListAsync();

        return Ok(suplementos);
    }

    public record ItemRequest(Guid? ProductoId, int Cantidad, decimal PrecioVentaUSD);
    public record CrearSuplementoRequest(Guid ContratoId, string Motivo, List<ItemRequest> Items);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearSuplementoRequest request)
    {
        var contrato = await _context.Contratos.FindAsync(request.ContratoId);
        if (contrato == null) return BadRequest("El contrato no existe.");

        var cantidadSuplementos = await _context.Suplementos
            .CountAsync(s => s.ContratoId == request.ContratoId && !s.IsDeleted);
        
        var numeroSuplemento = $"{contrato.NumeroContrato}-SUP-{cantidadSuplementos + 1:D2}";

        var suplemento = new Suplemento
        {
            ContratoId = request.ContratoId,
            NumeroSuplemento = numeroSuplemento,
            Estado = "Borrador",
            Motivo = request.Motivo,
            TotalUSD = 0
        };

        foreach (var item in request.Items)
        {
            var nuevoItem = new SuplementoItem
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioVentaUSD = item.PrecioVentaUSD,
                SubtotalUSD = item.Cantidad * item.PrecioVentaUSD
            };
            suplemento.Items.Add(nuevoItem);
            suplemento.TotalUSD += nuevoItem.SubtotalUSD;
        }

        _context.Suplementos.Add(suplemento);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Suplemento creado", id = suplemento.Id, numero = suplemento.NumeroSuplemento });
    }

    public record ActualizarEstadoRequest(string NuevoEstado);

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(Guid id, ActualizarEstadoRequest request)
    {
        var suplemento = await _context.Suplementos.FindAsync(id);
        if (suplemento == null) return NotFound("Suplemento no encontrado.");

        var estadosValidos = new[] { "Borrador", "Firmado", "Pagado", "Cancelado" };
        if (!estadosValidos.Contains(request.NuevoEstado))
            return BadRequest("Estado no válido.");

        suplemento.Estado = request.NuevoEstado;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Suplemento actualizado a {request.NuevoEstado}" });
    }
}