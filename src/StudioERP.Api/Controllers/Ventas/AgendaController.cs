using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using System.Security.Claims;

namespace StudioERP.Api.Controllers.Ventas;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AgendaController : ControllerBase
{
    private readonly StudioDbContext _context;

    public AgendaController(StudioDbContext context)
    {
        _context = context;
    }

    // 1. OBTENER EVENTOS PARA EL CALENDARIO
    [HttpGet("eventos")]
    public async Task<IActionResult> ObtenerEventos()
    {
        var query = _context.Sesiones
            .Include(s => s.Contrato).ThenInclude(c => c.Cliente)
            .Include(s => s.PersonalAsignado).ThenInclude(ps => ps.Staff).ThenInclude(st => st.Usuario)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        // Seguridad: Si no es Admin, solo ve sus eventos
        if (!User.IsInRole("Admin"))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                query = query.Where(s => s.PersonalAsignado.Any(ps => ps.Staff.UsuarioId == userId));
            }
            else
            {
                return Ok(new List<object>());
            }
        }

        var eventos = await query.Select(s => new
        {
            id = s.Id,
            title = $"{s.Contrato.Cliente.Nombre} - {s.Contrato.NumeroContrato}",
            start = s.FechaInicio.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = s.FechaFin.ToString("yyyy-MM-ddTHH:mm:ss"),
            extendedProps = new
            {
                locacion = s.Locacion,
                personal = s.PersonalAsignado.Select(ps => ps.Staff.Usuario.NombreCompleto).ToList()
            }
        }).ToListAsync();

        return Ok(eventos);
    }

    // 2. ASIGNAR STAFF (Se mantiene igual)
    public record AsignarStaffRequest(Guid SesionId, Guid StaffId, decimal ComisionPactadaUSD);

    [HttpPost("sesiones/asignar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AsignarStaff(AsignarStaffRequest request)
    {
        var sesion = await _context.Sesiones.FindAsync(request.SesionId);
        if (sesion == null) return BadRequest("La sesión no existe.");

        var staff = await _context.Staffs.Include(s => s.BloqueosAgenda).FirstOrDefaultAsync(s => s.Id == request.StaffId);
        if (staff == null) return BadRequest("El miembro del staff no existe.");

        var tieneBloqueo = staff.BloqueosAgenda.Any(b => b.FechaInicio <= sesion.FechaFin && b.FechaFin >= sesion.FechaInicio);
        if (tieneBloqueo) return BadRequest("El miembro del staff no está disponible en esa fecha.");

        var yaAsignado = await _context.SessionStaffs.AnyAsync(ss => ss.SesionId == request.SesionId && ss.StaffId == request.StaffId);
        if (yaAsignado) return BadRequest("El miembro del staff ya está asignado a esta sesión.");

        var asignacion = new SessionStaff
        {
            SesionId = request.SesionId,
            StaffId = request.StaffId,
            ComisionPactadaUSD = request.ComisionPactadaUSD
        };

        _context.SessionStaffs.Add(asignacion);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Staff asignado correctamente.", id = asignacion.Id });
    }

        // OBTENER BLOQUEOS DE AGENDA (Vacaciones, días libres)
    [HttpGet("bloqueos")]
    public async Task<IActionResult> ObtenerBloqueos()
    {
        var query = _context.DisponibilidadesStaff
            .Include(d => d.Staff).ThenInclude(s => s.Usuario)
            .AsQueryable();

        // Si no es Admin, solo ve sus propios bloqueos
        if (!User.IsInRole("Admin"))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                query = query.Where(d => d.Staff.UsuarioId == userId);
            }
        }

        var bloqueos = await query.Select(d => new
        {
            id = d.Id,
            title = $"Bloqueo: {d.Motivo} ({d.Staff.Usuario.NombreCompleto})",
            start = d.FechaInicio.ToString("yyyy-MM-dd"),
            end = d.FechaFin.AddDays(1).ToString("yyyy-MM-dd"), // FullCalendar requiere que el fin sea exclusivo
            color = "#ef4444" // Rojo para bloqueos
        }).ToListAsync();

        return Ok(bloqueos);
    }
}