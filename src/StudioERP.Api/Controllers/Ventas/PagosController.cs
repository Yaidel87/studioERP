using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Ventas;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PagosController : ControllerBase
{
    private readonly StudioDbContext _context;

    public PagosController(StudioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var pagos = await _context.Pagos
            .Include(p => p.Contrato).ThenInclude(c => c.Cliente)
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new
            {
                p.Id,
                Contrato = p.Contrato.NumeroContrato,
                Cliente = $"{p.Contrato.Cliente.Nombre} {p.Contrato.Cliente.Apellidos}",
                p.Monto,
                p.Moneda,
                p.MetodoPago,
                p.EstadoConciliacion,
                Fecha = p.FechaPago.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Ok(pagos);
    }

    public record PagoRequest(Guid ContratoId, decimal Monto, string Moneda, decimal TasaCambio, string MetodoPago, string ReferenciaExterna);

    [HttpPost]
    public async Task<IActionResult> Registrar(PagoRequest request)
    {
        var contrato = await _context.Contratos.FindAsync(request.ContratoId);
        if (contrato == null) return BadRequest("El contrato no existe.");

        var pago = new Pago
        {
            ContratoId = request.ContratoId,
            Monto = request.Monto,
            Moneda = request.Moneda,
            TasaCambio = request.TasaCambio,
            MetodoPago = request.MetodoPago,
            ReferenciaExterna = request.ReferenciaExterna,
            EstadoConciliacion = "PENDIENTE",
            FechaPago = DateTime.UtcNow
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pago registrado", id = pago.Id });
    }

    [HttpPut("{id}/verificar")]
    public async Task<IActionResult> Verificar(Guid id)
    {
        var pago = await _context.Pagos.FindAsync(id);
        if (pago == null) return NotFound("Pago no encontrado.");

        pago.EstadoConciliacion = "VERIFICADO";
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pago verificado correctamente." });
    }

    // RESUMEN FINANCIERO
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen()
    {
        var totalIngresos = await _context.Pagos
            .Where(p => p.EstadoConciliacion == "VERIFICADO" && !p.IsDeleted)
            .SumAsync(p => p.Monto);

        var totalComisiones = await _context.Comisiones
            .Where(c => c.Estado == "PENDIENTE" && !c.IsDeleted)
            .SumAsync(c => c.MontoComisionUSD);

        var totalGastos = await _context.Gastos
            .Where(g => !g.IsDeleted)
            .SumAsync(g => g.Monto);

        return Ok(new
        {
            Ingresos = totalIngresos,
            ComisionesPendientes = totalComisiones,
            Gastos = totalGastos,
            UtilidadBruta = totalIngresos - totalComisiones - totalGastos
        });
    }
}