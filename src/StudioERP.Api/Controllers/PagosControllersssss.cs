using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers;

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

    public record RegistrarPagoRequest(
        Guid ContratoId, 
        decimal Monto, 
        string Moneda, 
        decimal TasaCambio, 
        string MetodoPago, 
        string ReferenciaExterna);

    [HttpPost]
    public async Task<IActionResult> RegistrarPago(RegistrarPagoRequest request)
    {
        var contrato = await _context.Contratos.FindAsync(request.ContratoId);
        if (contrato == null) return BadRequest("El contrato no existe.");

        // Convertir el monto a USD (moneda base del contrato) si es necesario
        decimal montoEnUSD = request.Monto;
        if (request.Moneda == "CUP")
        {
            if (request.TasaCambio <= 0) return BadRequest("Debe proporcionar una tasa de cambio válida para CUP.");
            montoEnUSD = request.Monto / request.TasaCambio;
        }

        var pago = new Pago
        {
            ContratoId = request.ContratoId,
            Monto = request.Monto,
            Moneda = request.Moneda,
            TasaCambio = request.TasaCambio,
            MetodoPago = request.MetodoPago,
            ReferenciaExterna = request.ReferenciaExterna,
            EstadoConciliacion = "PENDIENTE", // Requiere verificación manual
            FechaPago = DateTime.UtcNow
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            mensaje = "Pago registrado. Pendiente de verificación.", 
            pagoId = pago.Id,
            montoEnUSD = montoEnUSD 
        });
    }

    [HttpGet("contrato/{contratoId}")]
    public async Task<IActionResult> ObtenerPagosPorContrato(Guid contratoId)
    {
        var pagos = await _context.Pagos
            .Where(p => p.ContratoId == contratoId && !p.IsDeleted)
            .ToListAsync();

        return Ok(pagos);
    }

    // Endpoint para que el Admin verifique el pago y se den por buenos los fondos
    [HttpPut("{pagoId}/verificar")]
    public async Task<IActionResult> VerificarPago(Guid pagoId)
    {
        var pago = await _context.Pagos.FindAsync(pagoId);
        if (pago == null) return NotFound("Pago no encontrado.");

        pago.EstadoConciliacion = "VERIFICADO";
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pago verificado correctamente." });
    }
}