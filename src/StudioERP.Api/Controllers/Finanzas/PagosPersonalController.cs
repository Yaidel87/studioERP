using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using StudioERP.Api.Services;

namespace StudioERP.Api.Controllers.Finanzas;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class PagosPersonalController : ControllerBase
{
    private readonly StudioDbContext _context;

    public PagosPersonalController(StudioDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var pagos = await _context.PagosPersonalIndirecto
            .Include(pago => pago.Staff).ThenInclude(staff => staff.Usuario)
            .Where(pago => !pago.IsDeleted)
            .OrderByDescending(pago => pago.Fecha)
            .Select(pago => new
            {
                pago.Id,
                pago.StaffId,
                Personal = pago.Staff.Usuario.NombreCompleto,
                pago.Monto,
                pago.MontoUSD,
                pago.TasaCambio,
                pago.Moneda,
                pago.Concepto,
                pago.Fecha,
                pago.Estado,
                pago.FechaPago
            })
            .ToListAsync();

        return Ok(pagos);
    }

    public record CrearPagoRequest(
        Guid StaffId,
        decimal Monto,
        string Moneda,
        string Concepto,
        DateTime Fecha);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearPagoRequest request)
    {
        var monto = Math.Round(request.Monto, 2, MidpointRounding.AwayFromZero);
        if (request.StaffId == Guid.Empty)
            return BadRequest("Debe seleccionar un miembro del personal.");
        if (monto <= 0)
            return BadRequest("El monto debe ser mayor que cero.");
        if (request.Moneda is not ("USD" or "CUP"))
            return BadRequest("La moneda debe ser USD o CUP.");
        if (string.IsNullOrWhiteSpace(request.Concepto))
            return BadRequest("El concepto del pago es obligatorio.");

        var personalExiste = await _context.Staffs
            .AnyAsync(staff => staff.Id == request.StaffId &&
                               !staff.IsDeleted &&
                               !staff.Usuario.IsDeleted &&
                               staff.Usuario.Activo);
        if (!personalExiste)
            return BadRequest("El miembro del personal no existe o está inactivo.");

        var pago = new PagoPersonalIndirecto
        {
            StaffId = request.StaffId,
            Monto = monto,
            MontoUSD = request.Moneda == "USD" ? monto : null,
            TasaCambio = request.Moneda == "USD" ? 1m : null,
            Moneda = request.Moneda,
            Concepto = request.Concepto.Trim(),
            Fecha = StudioClock.ToUtc(request.Fecha),
            Estado = "PENDIENTE"
        };
        _context.PagosPersonalIndirecto.Add(pago);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerTodos), new { id = pago.Id }, new { pago.Id });
    }

    public record ActualizarEstadoRequest(string Estado);

    [HttpPut("{id:guid}/estado")]
    public async Task<IActionResult> ActualizarEstado(Guid id, ActualizarEstadoRequest request)
    {
        if (request.Estado is not ("PENDIENTE" or "PAGADO"))
            return BadRequest("El estado debe ser PENDIENTE o PAGADO.");

        var pago = await _context.PagosPersonalIndirecto
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (pago == null)
            return NotFound("Pago no encontrado.");

        if (pago.Estado == "PAGADO" && request.Estado == "PENDIENTE")
            return Conflict("Un pago efectuado no se puede revertir a pendiente.");

        if (request.Estado == "PAGADO" && pago.Estado != "PAGADO" && pago.Moneda == "CUP")
        {
            var rate = await _context.TiposCambioDiarios
                .FirstOrDefaultAsync(item => !item.IsDeleted && item.Fecha == StudioClock.Today);
            if (rate == null)
                return BadRequest("No hay una tasa de cambio registrada para hoy.");

            pago.TasaCambio = rate.CupPorUsd;
            pago.MontoUSD = Math.Round(pago.Monto / rate.CupPorUsd, 2, MidpointRounding.AwayFromZero);
        }

        pago.Estado = request.Estado;
        pago.FechaPago = request.Estado == "PAGADO" ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var pago = await _context.PagosPersonalIndirecto
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (pago == null)
            return NotFound("Pago no encontrado.");

        pago.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
