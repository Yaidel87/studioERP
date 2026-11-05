using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;

namespace StudioERP.Api.Controllers.Seguridad;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class RolesController : ControllerBase
{
    private readonly StudioDbContext _context;

    public RolesController(StudioDbContext context)
    {
        _context = context;
    }

    // DTO Actualizado con el campo Permisos (string)
    public record RolRequest(string Nombre, string Descripcion, string Permisos);

    // 1. LISTAR ROLES
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var roles = await _context.Roles
            .Where(r => !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.Nombre,
                r.Descripcion,
                r.Permisos,
                Usuarios = r.Usuarios
                    .Where(u => !u.IsDeleted && u.Activo)
                    .Select(u => new { u.Id, u.Username, u.NombreCompleto })
                    .ToList(),
                CantidadUsuarios = r.Usuarios.Count(u => !u.IsDeleted && u.Activo)
            })
            .ToListAsync();

        return Ok(roles);
    }

    // 2. CREAR ROL
    [HttpPost]
    public async Task<IActionResult> Crear(RolRequest request)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest("El nombre del rol es obligatorio.");
        if (await _context.Roles.AnyAsync(r => r.Nombre == nombre && !r.IsDeleted))
            return BadRequest("El nombre del rol ya existe.");

        var rol = new Rol
        {
            Nombre = nombre,
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            Permisos = request.Permisos?.Trim() ?? string.Empty
        };

        _context.Roles.Add(rol);
        await _context.SaveChangesAsync();

        return Ok(rol);
    }

    // 3. ACTUALIZAR ROL
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, RolRequest request)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (rol == null) return NotFound("Rol no encontrado.");

        // Verificar que el nuevo nombre no esté en uso por otro rol
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest("El nombre del rol es obligatorio.");
        if (rol.Nombre != nombre && await _context.Roles.AnyAsync(r => r.Nombre == nombre && !r.IsDeleted))
            return BadRequest("Ya existe otro rol con ese nombre.");

        rol.Nombre = nombre;
        rol.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
        rol.Permisos = request.Permisos?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync();

        return Ok(rol);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (rol == null) return NotFound("Rol no encontrado.");
        if (await _context.Usuarios.AnyAsync(u => u.RolId == id && !u.IsDeleted && u.Activo))
            return BadRequest("No se puede eliminar un rol con usuarios activos asignados.");

        rol.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Rol eliminado correctamente." });
    }
}