using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using StudioERP.Api.Services;

namespace StudioERP.Api.Controllers.Ventas;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class GastosController : ControllerBase
{
    private readonly StudioDbContext _context;

    public GastosController(StudioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] int? anio, [FromQuery] int? mes)
    {
        if (anio.HasValue != mes.HasValue ||
            (mes.HasValue && (mes < 1 || mes > 12)) ||
            (anio.HasValue && (anio < 1 || anio > 9999)))
            return BadRequest("Indica un año y un mes válidos para consultar el período.");

        var query = _context.Gastos.Where(gasto => !gasto.IsDeleted);
        if (anio.HasValue && mes.HasValue)
        {
            var inicioLocal = new DateTime(anio.Value, mes.Value, 1);
            var inicio = StudioClock.ToUtc(inicioLocal);
            var fin = StudioClock.ToUtc(inicioLocal.AddMonths(1));
            query = query.Where(gasto => gasto.Fecha >= inicio && gasto.Fecha < fin);
        }

        var gastos = await query
            .OrderByDescending(g => g.Fecha)
            .Select(g => new
            {
                g.Id,
                g.Categoria,
                g.Descripcion,
                g.Monto,
                g.Moneda,
                g.MontoUSD,
                g.TasaCambio,
                g.TipoMovimiento,
                g.Fecha
            })
            .ToListAsync();
        return Ok(gastos);
    }

    public record GastoRequest(
        string Categoria,
        string Descripcion,
        decimal Monto,
        string Moneda,
        DateTime Fecha,
        string TipoMovimiento = "GastoOperativo");

    [HttpPost]
    public async Task<IActionResult> Crear(GastoRequest request)
    {
        var monto = Math.Round(request.Monto, 2, MidpointRounding.AwayFromZero);
        if (monto <= 0)
            return BadRequest("El monto debe ser mayor que cero.");
        if (request.Moneda is not ("USD" or "CUP"))
            return BadRequest("La moneda debe ser USD o CUP.");
        if (request.TipoMovimiento is not ("GastoOperativo" or "PagoDeuda" or "Reinversion"))
            return BadRequest("El tipo de movimiento no es válido.");
        if (string.IsNullOrWhiteSpace(request.Categoria) || string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("La categoría y la descripción son obligatorias.");

        var fechaLocal = DateOnly.FromDateTime(request.Fecha);
        var tasaCambio = 1m;
        decimal? montoUSD = monto;
        if (request.Moneda == "CUP")
        {
            var rate = await _context.TiposCambioDiarios
                .FirstOrDefaultAsync(item => !item.IsDeleted && item.Fecha == fechaLocal);
            if (rate == null)
                return BadRequest($"No hay una tasa de cambio registrada para el {fechaLocal:dd/MM/yyyy}.");

            tasaCambio = rate.CupPorUsd;
            montoUSD = Math.Round(monto / tasaCambio, 2, MidpointRounding.AwayFromZero);
        }

        var gasto = new Gasto
        {
            Categoria = request.Categoria.Trim(),
            Descripcion = request.Descripcion.Trim(),
            Monto = monto,
            Moneda = request.Moneda,
            MontoUSD = montoUSD,
            TasaCambio = tasaCambio,
            TipoMovimiento = request.TipoMovimiento,
            Fecha = StudioClock.ToUtc(request.Fecha)
        };
        _context.Gastos.Add(gasto);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Gasto registrado", id = gasto.Id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var gasto = await _context.Gastos.FindAsync(id);
        if (gasto == null) return NotFound("Gasto no encontrado.");
        gasto.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Gasto eliminado" });
    }
}