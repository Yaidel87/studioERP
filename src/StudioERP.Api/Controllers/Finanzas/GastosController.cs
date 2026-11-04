using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

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
    public async Task<IActionResult> ObtenerTodos()
    {
        var gastos = await _context.Gastos
            .Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.Fecha)
            .ToListAsync();
        return Ok(gastos);
    }

    public record GastoRequest(string Categoria, string Descripcion, decimal Monto, string Moneda, DateTime Fecha);

    [HttpPost]
    public async Task<IActionResult> Crear(GastoRequest request)
    {
        var gasto = new Gasto
        {
            Categoria = request.Categoria,
            Descripcion = request.Descripcion,
            Monto = request.Monto,
            Moneda = request.Moneda,
            Fecha = request.Fecha.ToUniversalTime()
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