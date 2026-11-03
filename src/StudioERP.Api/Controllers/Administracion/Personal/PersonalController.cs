using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Administracion.Personal;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class PersonalController : ControllerBase
{
    private readonly StudioDbContext _context;

    public PersonalController(StudioDbContext context)
    {
        _context = context;
    }

    // 1. LISTAR TODO EL STAFF
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var staff = await _context.Staffs
            .Include(s => s.Usuario)
            .Include(s => s.TiposStaff)
            .Where(s => !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.UsuarioId,
                NombreUsuario = s.Usuario.NombreCompleto,
                Email = s.Usuario.Email,
                Foto = s.Usuario.PhotoUrl,
                Especialidades = s.TiposStaff.Select(t => t.Nombre).ToList(),
                EspecialidadIds = s.TiposStaff.Select(t => t.Id).ToList()
            })
            .ToListAsync();

        return Ok(staff);
    }

    // 2. CREAR STAFF (Con la entidad especificada para evitar conflictos)
    public record CrearStaffRequest(Guid UsuarioId, List<Guid> TipoStaffIds);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearStaffRequest request)
    {
        var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == request.UsuarioId);
        if (!usuarioExiste) return BadRequest("El usuario no existe.");

        var yaEsStaff = await _context.Staffs.AnyAsync(s => s.UsuarioId == request.UsuarioId);
        if (yaEsStaff) return BadRequest("Este usuario ya está registrado como staff.");

        // Usamos la ruta completa para evitar el conflicto con el namespace
        var staff = new StudioERP.Domain.Entities.Staff 
        { 
            UsuarioId = request.UsuarioId 
        };

        var tipos = await _context.TiposStaff
            .Where(t => request.TipoStaffIds.Contains(t.Id))
            .ToListAsync();

        foreach (var tipo in tipos)
        {
            staff.TiposStaff.Add(tipo);
        }

        _context.Staffs.Add(staff);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Staff creado correctamente", id = staff.Id });
    }

    // 3. ACTUALIZAR ESPECIALIDADES
    public record ActualizarEspecialidadesRequest(List<Guid> TipoStaffIds);

    [HttpPut("{id}/especialidades")]
    public async Task<IActionResult> ActualizarEspecialidades(Guid id, ActualizarEspecialidadesRequest request)
    {
        var staff = await _context.Staffs
            .Include(s => s.TiposStaff)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (staff == null) return NotFound("Staff no encontrado.");

        staff.TiposStaff.Clear();

        var nuevosTipos = await _context.TiposStaff
            .Where(t => request.TipoStaffIds.Contains(t.Id))
            .ToListAsync();

        foreach (var tipo in nuevosTipos)
        {
            staff.TiposStaff.Add(tipo);
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Especialidades actualizadas correctamente." });
    }

    // ==========================================
    // --- GESTIÓN DE TIPOS DE STAFF (ESPECIALIDADES) ---
    // ==========================================

    [HttpGet("tipos")]
    public async Task<IActionResult> ObtenerTipos()
    {
        var tipos = await _context.TiposStaff.ToListAsync();
        return Ok(tipos);
    }

    public record TipoStaffRequest(string Nombre);

    [HttpPost("tipos")]
    public async Task<IActionResult> CrearTipo(TipoStaffRequest request)
    {
        if (await _context.TiposStaff.AnyAsync(t => t.Nombre == request.Nombre))
            return BadRequest("El tipo de staff ya existe.");

        var tipo = new TipoStaff { Nombre = request.Nombre };
        _context.TiposStaff.Add(tipo);
        await _context.SaveChangesAsync();

        return Ok(tipo);
    }

    [HttpDelete("tipos/{id}")]
    public async Task<IActionResult> EliminarTipo(Guid id)
    {
        var tipo = await _context.TiposStaff
            .Include(t => t.Staffs)
            .FirstOrDefaultAsync(t => t.Id == id);
            
        if (tipo == null) return NotFound("Especialidad no encontrada.");
        
        if (tipo.Staffs.Any())
            return BadRequest("No se puede eliminar la especialidad porque tiene personal asignado.");

        _context.TiposStaff.Remove(tipo);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Especialidad eliminada correctamente." });
    }
}