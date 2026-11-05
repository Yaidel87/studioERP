using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using StudioERP.Api.Services;

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

    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var pagos = await _context.Pagos
            .Include(p => p.Contrato).ThenInclude(c => c.Cliente)
            .Where(p => !p.IsDeleted && !p.Contrato.IsDeleted)
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new
            {
                p.Id,
                Contrato = p.Contrato.NumeroContrato,
                Cliente = $"{p.Contrato.Cliente.Nombre} {p.Contrato.Cliente.Apellidos}",
                p.Monto,
                p.MontoUSD,
                p.Moneda,
                p.TasaCambio,
                p.MetodoPago,
                p.EstadoConciliacion,
                Fecha = p.FechaPago.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Ok(pagos);
    }

    public record PagoRequest(Guid ContratoId, decimal Monto, string Moneda, string MetodoPago, string ReferenciaExterna);

    [Authorize(Roles = "Admin,Cajero")]
    [HttpPost]
    public async Task<IActionResult> Registrar(PagoRequest request)
    {
        var monto = Math.Round(request.Monto, 2, MidpointRounding.AwayFromZero);
        if (monto <= 0)
            return BadRequest("El monto debe ser mayor que cero.");
        if (request.Moneda is not ("USD" or "CUP"))
            return BadRequest("La moneda debe ser USD o CUP.");
        if (string.IsNullOrWhiteSpace(request.MetodoPago))
            return BadRequest("El método de pago es obligatorio.");

        var fechaPago = DateTime.UtcNow;
        var tasaCambio = 1m;
        var montoUSD = monto;
        if (request.Moneda == "CUP")
        {
            var fechaTasa = StudioClock.Today;
            var tasaDiaria = await _context.TiposCambioDiarios
                .Where(rate => !rate.IsDeleted && rate.Fecha == fechaTasa)
                .FirstOrDefaultAsync();
            if (tasaDiaria == null)
                return BadRequest("No hay una tasa de cambio registrada para hoy.");

            tasaCambio = tasaDiaria.CupPorUsd;
            montoUSD = Math.Round(monto / tasaCambio, 2, MidpointRounding.AwayFromZero);
        }

        var contrato = await _context.Contratos
            .FirstOrDefaultAsync(item => item.Id == request.ContratoId && !item.IsDeleted);
        if (contrato == null) return BadRequest("El contrato no existe o está inactivo.");

        var pago = new Pago
        {
            ContratoId = request.ContratoId,
            Monto = monto,
            MontoUSD = montoUSD,
            Moneda = request.Moneda,
            TasaCambio = tasaCambio,
            MetodoPago = request.MetodoPago,
            ReferenciaExterna = request.ReferenciaExterna,
            EstadoConciliacion = "PENDIENTE",
            FechaPago = fechaPago
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pago registrado", id = pago.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/verificar")]
    public async Task<IActionResult> Verificar(Guid id)
    {
        var pago = await _context.Pagos
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (pago == null) return NotFound("Pago no encontrado.");

        pago.EstadoConciliacion = "VERIFICADO";
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Pago verificado correctamente." });
    }

    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet("contrato/{contratoId}")]
    public async Task<IActionResult> ObtenerPagosPorContrato(Guid contratoId)
    {
        var pagos = await _context.Pagos
            .Where(p => p.ContratoId == contratoId && !p.IsDeleted)
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new
            {
                p.Id,
                p.Monto,
                p.Moneda,
                p.TasaCambio,
                p.MetodoPago,
                p.ReferenciaExterna,
                p.EstadoConciliacion,
                p.FechaPago
            })
            .ToListAsync();

        return Ok(pagos);
    }

    // RESUMEN FINANCIERO
    [Authorize(Roles = "Admin,Cajero")]
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen([FromQuery] int? anio, [FromQuery] int? mes)
    {
        if (anio.HasValue != mes.HasValue ||
            (mes.HasValue && (mes < 1 || mes > 12)) ||
            (anio.HasValue && (anio < 1 || anio > 9999)))
            return BadRequest("Indica un año y un mes válidos para consultar el período.");

        DateTime? inicio = null;
        DateTime? fin = null;
        if (anio.HasValue && mes.HasValue)
        {
            var inicioLocal = new DateTime(anio.Value, mes.Value, 1);
            inicio = StudioClock.ToUtc(inicioLocal);
            fin = StudioClock.ToUtc(inicioLocal.AddMonths(1));
        }

        var pagosQuery = _context.Pagos.Where(payment =>
            payment.EstadoConciliacion == "VERIFICADO" && !payment.IsDeleted);
        var gastosQuery = _context.Gastos.Where(expense => !expense.IsDeleted);
        var pagosPersonalQuery = _context.PagosPersonalIndirecto.Where(payment => !payment.IsDeleted);
        var comisionesQuery = _context.Comisiones.Where(commission =>
            commission.Estado == "PENDIENTE" && !commission.IsDeleted);
        if (inicio.HasValue && fin.HasValue)
        {
            pagosQuery = pagosQuery.Where(payment =>
                payment.FechaPago >= inicio.Value && payment.FechaPago < fin.Value);
            gastosQuery = gastosQuery.Where(expense =>
                expense.Fecha >= inicio.Value && expense.Fecha < fin.Value);
            pagosPersonalQuery = pagosPersonalQuery.Where(payment =>
                (payment.Estado == "PAGADO" &&
                 payment.FechaPago.HasValue &&
                 payment.FechaPago.Value >= inicio.Value &&
                 payment.FechaPago.Value < fin.Value) ||
                (payment.Estado == "PENDIENTE" && payment.Fecha >= inicio.Value && payment.Fecha < fin.Value));
            comisionesQuery = comisionesQuery.Where(commission =>
                commission.SessionStaff.Sesion.FechaInicio >= inicio.Value &&
                commission.SessionStaff.Sesion.FechaInicio < fin.Value);
        }

        var totalIngresos = await pagosQuery.SumAsync(payment => payment.MontoUSD) ?? 0m;

        var totalComisiones = await comisionesQuery.SumAsync(commission => commission.MontoComisionUSD);

        var gastosOperativosUSD = await gastosQuery
            .Where(expense => expense.TipoMovimiento == "GastoOperativo")
            .SumAsync(expense => expense.MontoUSD) ?? 0m;
        var pagosPersonalPendientesUSD = await pagosPersonalQuery
            .Where(payment => payment.Estado == "PENDIENTE")
            .SumAsync(payment => payment.MontoUSD) ?? 0m;
        var pagosPersonalPendientesCUP = await pagosPersonalQuery
            .Where(payment => payment.Estado == "PENDIENTE" && payment.Moneda == "CUP")
            .SumAsync(payment => (decimal?)payment.Monto) ?? 0m;
        var pagosPersonalPagadosUSD = await pagosPersonalQuery
            .Where(payment => payment.Estado == "PAGADO")
            .SumAsync(payment => payment.MontoUSD) ?? 0m;
        var pagosDeudaUSD = await gastosQuery
            .Where(expense => expense.TipoMovimiento == "PagoDeuda")
            .SumAsync(expense => expense.MontoUSD) ?? 0m;
        var reinversionUSD = await gastosQuery
            .Where(expense => expense.TipoMovimiento == "Reinversion")
            .SumAsync(expense => expense.MontoUSD) ?? 0m;
        var registrosCUPHistoricosSinConversion =
            await pagosQuery.CountAsync(payment => payment.Moneda == "CUP" && payment.MontoUSD == null) +
            await gastosQuery.CountAsync(expense => expense.Moneda == "CUP" && expense.MontoUSD == null) +
            await pagosPersonalQuery.CountAsync(payment =>
                payment.Estado == "PAGADO" && payment.Moneda == "CUP" && payment.MontoUSD == null);

        return Ok(new
        {
            Anio = anio,
            Mes = mes,
            Ingresos = totalIngresos,
            ComisionesPendientesUSD = totalComisiones,
            GastosOperativosUSD = gastosOperativosUSD,
            PagosPersonalPendientesUSD = pagosPersonalPendientesUSD,
            PagosPersonalPendientesCUP = pagosPersonalPendientesCUP,
            PagosPersonalPagadosUSD = pagosPersonalPagadosUSD,
            PagosDeudaUSD = pagosDeudaUSD,
            ReinversionUSD = reinversionUSD,
            RegistrosCUPHistoricosSinConversion = registrosCUPHistoricosSinConversion,
            UtilidadOperativaUSD = totalIngresos - totalComisiones - gastosOperativosUSD - pagosPersonalPagadosUSD
        });
    }
}