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
                CantidadUsuarios = r.Usuarios.Count
            })
            .ToListAsync();

        return Ok(roles);
    }

    // 2. CREAR ROL
    [HttpPost]
    public async Task<IActionResult> Crear(RolRequest request)
    {
        if (await _context.Roles.AnyAsync(r => r.Nombre == request.Nombre))
            return BadRequest("El nombre del rol ya existe.");

        var rol = new Rol
        {
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Permisos = request.Permisos // Guardamos los permisos como string separado por comas
        };

        _context.Roles.Add(rol);
        await _context.SaveChangesAsync();

        return Ok(rol);
    }

    // 3. ACTUALIZAR ROL
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, RolRequest request)
    {
        var rol = await _context.Roles.FindAsync(id);
        if (rol == null) return NotFound("Rol no encontrado.");

        // Verificar que el nuevo nombre no esté en uso por otro rol
        if (rol.Nombre != request.Nombre && await _context.Roles.AnyAsync(r => r.Nombre == request.Nombre))
            return BadRequest("Ya existe otro rol con ese nombre.");

        rol.Nombre = request.Nombre;
        rol.Descripcion = request.Descripcion;
        rol.Permisos = request.Permisos;

        await _context.SaveChangesAsync();

        return Ok(rol);
    }
}