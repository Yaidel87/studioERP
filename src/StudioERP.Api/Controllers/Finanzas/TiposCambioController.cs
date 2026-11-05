using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using StudioERP.Api.Services;

namespace StudioERP.Api.Controllers.Finanzas;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class TiposCambioController : ControllerBase
{
    private readonly StudioDbContext _context;

    public TiposCambioController(StudioDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet]
    public async Task<IActionResult> ObtenerHistorial()
    {
        var historial = await _context.TiposCambioDiarios
            .Where(rate => !rate.IsDeleted)
            .OrderByDescending(rate => rate.Fecha)
            .Select(rate => new { rate.Id, rate.Fecha, rate.CupPorUsd })
            .ToListAsync();
        return Ok(historial);
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet("actual")]
    public async Task<IActionResult> ObtenerActual()
    {
        var actual = await ObtenerTipoCambioDelDia(StudioClock.Today);
        return actual == null
            ? NotFound("No hay una tasa de cambio registrada para la fecha actual.")
            : Ok(new { actual.Fecha, actual.CupPorUsd });
    }

    public record RegistrarTasaRequest(DateOnly Fecha, decimal CupPorUsd);

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> RegistrarTasa(RegistrarTasaRequest request)
    {
        var cupPorUsd = Math.Round(request.CupPorUsd, 4, MidpointRounding.AwayFromZero);
        if (cupPorUsd <= 0)
            return BadRequest("La tasa CUP por USD debe ser mayor que cero.");
        if (request.Fecha > StudioClock.Today)
            return BadRequest("No se puede registrar una tasa para una fecha futura.");
        if (await _context.TiposCambioDiarios.AnyAsync(rate => rate.Fecha == request.Fecha && !rate.IsDeleted))
            return Conflict("Ya existe una tasa registrada para esa fecha. El historial no se puede sobrescribir.");

        var rate = new TipoCambioDiario
        {
            Fecha = request.Fecha,
            CupPorUsd = cupPorUsd
        };
        _context.TiposCambioDiarios.Add(rate);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(ObtenerActual), new { }, new { rate.Id, rate.Fecha, rate.CupPorUsd });
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet("saldo-cup")]
    public async Task<IActionResult> ObtenerSaldoCUP()
    {
        var balance = await ObtenerSaldoCUPInterno();
        return Ok(new { SaldoCUP = balance });
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet("cambios")]
    public async Task<IActionResult> ObtenerCambios()
    {
        var cambios = await _context.CambiosMoneda
            .Where(change => !change.IsDeleted)
            .OrderByDescending(change => change.Fecha)
            .Select(change => new
            {
                change.Id,
                change.Fecha,
                change.MontoCUP,
                change.CupPorUsd,
                change.MontoUSDRecibido
            })
            .ToListAsync();
        return Ok(cambios);
    }

    public record CambiarMonedaRequest(decimal MontoCUP);

    [Authorize(Roles = "Admin")]
    [HttpPost("cambios")]
    public async Task<IActionResult> CambiarCUPaUSD(CambiarMonedaRequest request)
    {
        var montoCup = Math.Round(request.MontoCUP, 2, MidpointRounding.AwayFromZero);
        if (montoCup <= 0)
            return BadRequest("El monto CUP a cambiar debe ser mayor que cero.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var rate = await ObtenerTipoCambioDelDia(StudioClock.Today);
        if (rate == null)
            return BadRequest("Registra la tasa de cambio del día antes de cambiar moneda.");

        var saldoCup = await ObtenerSaldoCUPInterno();
        if (montoCup > saldoCup)
            return BadRequest($"El cambio supera el saldo disponible de {saldoCup:N2} CUP.");

        var cambio = new CambioMoneda
        {
            Fecha = DateTime.UtcNow,
            MontoCUP = montoCup,
            CupPorUsd = rate.CupPorUsd,
            MontoUSDRecibido = Math.Round(montoCup / rate.CupPorUsd, 2, MidpointRounding.AwayFromZero)
        };
        _context.CambiosMoneda.Add(cambio);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            cambio.Id,
            cambio.Fecha,
            cambio.MontoCUP,
            cambio.CupPorUsd,
            cambio.MontoUSDRecibido,
            SaldoCUP = saldoCup - cambio.MontoCUP
        });
    }

    private Task<TipoCambioDiario?> ObtenerTipoCambioDelDia(DateOnly fecha) =>
        _context.TiposCambioDiarios
            .Where(rate => !rate.IsDeleted && rate.Fecha == fecha)
            .FirstOrDefaultAsync();

    private async Task<decimal> ObtenerSaldoCUPInterno()
    {
        var cobrosCup = await _context.Pagos
            .Where(payment => !payment.IsDeleted &&
                              payment.EstadoConciliacion == "VERIFICADO" &&
                              payment.Moneda == "CUP")
            .SumAsync(payment => (decimal?)payment.Monto) ?? 0m;
        var gastosCup = await _context.Gastos
            .Where(expense => !expense.IsDeleted && expense.Moneda == "CUP")
            .SumAsync(expense => (decimal?)expense.Monto) ?? 0m;
        var pagosPersonalCup = await _context.PagosPersonalIndirecto
            .Where(payment => !payment.IsDeleted &&
                              payment.Estado == "PAGADO" &&
                              payment.Moneda == "CUP")
            .SumAsync(payment => (decimal?)payment.Monto) ?? 0m;
        var cupCambiado = await _context.CambiosMoneda
            .Where(change => !change.IsDeleted)
            .SumAsync(change => (decimal?)change.MontoCUP) ?? 0m;
        return cobrosCup - gastosCup - pagosPersonalCup - cupCambiado;
    }
}
