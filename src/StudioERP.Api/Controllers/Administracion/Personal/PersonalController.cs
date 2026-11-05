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
            .Where(s => !s.IsDeleted && !s.Usuario.IsDeleted && s.Usuario.Activo)
            .Select(s => new
            {
                s.Id,
                s.UsuarioId,
                NombreUsuario = s.Usuario.NombreCompleto,
                Email = s.Usuario.Email,
                Foto = s.Usuario.PhotoUrl,
                Especialidades = s.TiposStaff.Where(t => !t.IsDeleted).Select(t => t.Nombre).ToList(),
                EspecialidadIds = s.TiposStaff.Where(t => !t.IsDeleted).Select(t => t.Id).ToList()
            })
            .ToListAsync();

        return Ok(staff);
    }

    // 2. CREAR STAFF (Con la entidad especificada para evitar conflictos)
    public record CrearStaffRequest(Guid UsuarioId, List<Guid>? TipoStaffIds);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearStaffRequest request)
    {
        if (request.UsuarioId == Guid.Empty)
            return BadRequest("Debe seleccionar un usuario válido.");
        var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == request.UsuarioId && !u.IsDeleted && u.Activo);
        if (!usuarioExiste) return BadRequest("El usuario no existe o está inactivo.");

        var tipoIds = request.TipoStaffIds?.Distinct().ToList() ?? new List<Guid>();
        var tipos = await _context.TiposStaff
            .Where(t => !t.IsDeleted && tipoIds.Contains(t.Id))
            .ToListAsync();
        if (tipos.Count != tipoIds.Count)
            return BadRequest("Una o más especialidades no existen o están inactivas.");

        var yaEsStaff = await _context.Staffs.AnyAsync(s => s.UsuarioId == request.UsuarioId && !s.IsDeleted);
        if (yaEsStaff) return BadRequest("Este usuario ya está registrado como staff.");

        var staff = await _context.Staffs
            .Include(s => s.TiposStaff)
            .FirstOrDefaultAsync(s => s.UsuarioId == request.UsuarioId && s.IsDeleted);
        if (staff == null)
        {
            staff = new StudioERP.Domain.Entities.Staff { UsuarioId = request.UsuarioId };
            _context.Staffs.Add(staff);
        }
        else
        {
            staff.IsDeleted = false;
            staff.TiposStaff.Clear();
        }

        foreach (var tipo in tipos)
            staff.TiposStaff.Add(tipo);

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Staff creado correctamente", id = staff.Id });
    }

    // 3. ACTUALIZAR ESPECIALIDADES
    public record ActualizarEspecialidadesRequest(List<Guid>? TipoStaffIds);

    [HttpPut("{id}/especialidades")]
    public async Task<IActionResult> ActualizarEspecialidades(Guid id, ActualizarEspecialidadesRequest request)
    {
        var staff = await _context.Staffs
            .Include(s => s.TiposStaff)
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            
        if (staff == null) return NotFound("Staff no encontrado.");
        if (staff.Usuario.IsDeleted || !staff.Usuario.Activo)
            return BadRequest("No se pueden actualizar especialidades de un usuario inactivo.");

        var tipoIds = request.TipoStaffIds?.Distinct().ToList() ?? new List<Guid>();
        var nuevosTipos = await _context.TiposStaff
            .Where(t => !t.IsDeleted && tipoIds.Contains(t.Id))
            .ToListAsync();
        if (nuevosTipos.Count != tipoIds.Count)
            return BadRequest("Una o más especialidades no existen o están inactivas.");

        staff.TiposStaff.Clear();
        foreach (var tipo in nuevosTipos)
            staff.TiposStaff.Add(tipo);

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Especialidades actualizadas correctamente." });
    }

    // ==========================================
    // --- GESTIÓN DE TIPOS DE STAFF (ESPECIALIDADES) ---
    // ==========================================

    [HttpGet("tipos")]
    public async Task<IActionResult> ObtenerTipos()
    {
        var tipos = await _context.TiposStaff.Where(t => !t.IsDeleted).ToListAsync();
        return Ok(tipos);
    }

    public record TipoStaffRequest(string Nombre);

    [HttpPost("tipos")]
    public async Task<IActionResult> CrearTipo(TipoStaffRequest request)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest("El nombre de la especialidad es obligatorio.");
        if (await _context.TiposStaff.AnyAsync(t => t.Nombre == nombre && !t.IsDeleted))
            return BadRequest("El tipo de staff ya existe.");

        var tipo = new TipoStaff { Nombre = nombre };
        _context.TiposStaff.Add(tipo);
        await _context.SaveChangesAsync();

        return Ok(tipo);
    }

    [HttpDelete("tipos/{id}")]
    public async Task<IActionResult> EliminarTipo(Guid id)
    {
        var tipo = await _context.TiposStaff
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            
        if (tipo == null) return NotFound("Especialidad no encontrada.");
        
        var tienePersonalActivo = await _context.Staffs.AnyAsync(s =>
            s.TiposStaff.Any(t => t.Id == id) &&
            !s.IsDeleted &&
            !s.Usuario.IsDeleted &&
            s.Usuario.Activo);
        if (tienePersonalActivo)
            return BadRequest("No se puede eliminar la especialidad porque tiene personal asignado.");

        tipo.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Especialidad eliminada correctamente." });
    }
}