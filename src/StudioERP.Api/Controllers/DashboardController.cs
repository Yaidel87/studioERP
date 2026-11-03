using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Infrastructure.Data;
using System.Security.Claims;

namespace StudioERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly StudioDbContext _context;

    public DashboardController(StudioDbContext context)
    {
        _context = context;
    }

    // 1. RESUMEN (Para las tarjetas de arriba)
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen()
    {
        var totalClientes = await _context.Clientes.CountAsync(c => !c.IsDeleted);
        var totalProductos = await _context.Productos.CountAsync(p => !p.IsDeleted);
        var contratosPendientes = await _context.Contratos.CountAsync(c => c.Estado == "Borrador" && !c.IsDeleted);
        
        var hoy = DateTime.UtcNow.Date;
        var sesionesHoy = await _context.Sesiones.CountAsync(s => s.FechaInicio.Date == hoy && !s.IsDeleted);

        return Ok(new
        {
            Clientes = totalClientes,
            Productos = totalProductos,
            ContratosPendientes = contratosPendientes,
            SesionesHoy = sesionesHoy
        });
    }

    // 2. PRÓXIMAS SESIONES (Para la lista de abajo)
    [HttpGet("proximas-sesiones")]
    public async Task<IActionResult> ObtenerProximasSesiones()
    {
        var hoy = DateTime.UtcNow.Date;
        var enTreintaDias = hoy.AddDays(30);

        var query = _context.Sesiones
            .Include(s => s.Contrato).ThenInclude(c => c.Cliente)
            .Where(s => !s.IsDeleted && s.FechaInicio >= hoy && s.FechaInicio <= enTreintaDias)
            .AsQueryable();

        // Si no es Admin, solo ve sus sesiones
        if (!User.IsInRole("Admin"))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                query = query.Where(s => s.PersonalAsignado.Any(ps => ps.Staff.UsuarioId == userId));
            }
        }

        var sesiones = await query
            .OrderBy(s => s.FechaInicio)
            .Take(10) // Solo las 10 más próximas
            .Select(s => new
            {
                s.Id,
                Fecha = s.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                Cliente = $"{s.Contrato.Cliente.Nombre} {s.Contrato.Cliente.Apellidos}",
                Contrato = s.Contrato.NumeroContrato,
                s.Locacion
            })
            .ToListAsync();

        return Ok(sesiones);
    }
}