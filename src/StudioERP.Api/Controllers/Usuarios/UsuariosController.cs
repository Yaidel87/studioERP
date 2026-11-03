using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using StudioERP.Infrastructure.Services;

namespace StudioERP.Api.Controllers.Usuarios;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly StudioDbContext _context;
    private readonly AuthService _authService;
    private readonly IWebHostEnvironment _env;

    public UsuariosController(StudioDbContext context, AuthService authService, IWebHostEnvironment env)
    {
        _context = context;
        _authService = authService;
        _env = env;
    }

    // 1. LISTAR USUARIOS
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.NombreCompleto,
                u.Email,
                u.PhotoUrl,
                u.Activo,
                Rol = u.Rol.Nombre
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    // 2. CREAR USUARIO (Con automatización de Staff y múltiples especialidades)
    public record CrearUsuarioRequest(string Username, string NombreCompleto, string Email, string Password, Guid RolId, bool Activo);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
            return BadRequest("El nombre de usuario ya existe.");

        var usuario = new Usuario
        {
            Username = request.Username,
            NombreCompleto = request.NombreCompleto,
            Email = request.Email,
            RolId = request.RolId,
            Activo = request.Activo
        };

        usuario.PasswordHash = _authService.HashPassword(usuario, request.Password);
        
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // --- LÓGICA AUTOMÁTICA PARA STAFF ---
        if (usuario.Activo)
        {
            var rol = await _context.Roles.FindAsync(usuario.RolId);
            
            if (rol != null && (rol.Nombre == "Fotógrafo" || rol.Nombre == "Maquillador" || rol.Nombre == "Editor"))
            {
                var tipoStaff = await _context.TiposStaff.FirstOrDefaultAsync(t => t.Nombre == rol.Nombre);
                
                if (tipoStaff == null)
                {
                    tipoStaff = new TipoStaff { Nombre = rol.Nombre };
                    _context.TiposStaff.Add(tipoStaff);
                    await _context.SaveChangesAsync();
                }

                // Se crea el staff y se le asigna la especialidad
                var staff = new Staff
                {
                    UsuarioId = usuario.Id
                };
                
                // AQUÍ ESTÁ EL CAMBIO CLAVE: Añadir a la lista en lugar de asignar un ID
                staff.TiposStaff.Add(tipoStaff);

                _context.Staffs.Add(staff);
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { mensaje = "Usuario creado exitosamente", id = usuario.Id });
    }

    // 3. SUBIR FOTO DEL USUARIO
    [HttpPost("{id}/foto")]
    public async Task<IActionResult> SubirFoto(Guid id, IFormFile archivo)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        if (archivo == null || archivo.Length == 0)
            return BadRequest("No se envió ninguna imagen.");

        try
        {
            var basePath = Directory.GetCurrentDirectory();
            var uploadsFolder = Path.Combine(basePath, "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            usuario.PhotoUrl = $"/uploads/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Foto actualizada", url = usuario.PhotoUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // 4. OBTENER ROLES
    [HttpGet("roles")]
    public async Task<IActionResult> ObtenerRoles()
    {
        var roles = await _context.Roles.Select(r => new { r.Id, r.Nombre }).ToListAsync();
        return Ok(roles);
    }

    // 5. CAMBIAR CONTRASEÑA (Usuario logueado)
    public record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);

    [HttpPut("{id}/password")]
    public async Task<IActionResult> CambiarPassword(Guid id, CambiarPasswordRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        if (!_authService.VerifyPassword(usuario, request.PasswordActual))
            return BadRequest("La contraseña actual es incorrecta.");

        usuario.PasswordHash = _authService.HashPassword(usuario, request.PasswordNueva);
        
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contraseña actualizada correctamente." });
    }

    // 6. ACTUALIZAR PERFIL (Solo el propio usuario)
    public record ActualizarPerfilRequest(string NombreCompleto, string Email);

    [HttpPut("{id}/perfil")]
    public async Task<IActionResult> ActualizarPerfil(Guid id, ActualizarPerfilRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        usuario.NombreCompleto = request.NombreCompleto;
        usuario.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Perfil actualizado correctamente." });
    }

    // 7. EDITAR USUARIO (ADMIN - Todo completo)
    public record EditarUsuarioAdminRequest(string Username, string NombreCompleto, string Email, Guid RolId, bool Activo);

    [HttpPut("{id}/admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditarUsuarioAdmin(Guid id, EditarUsuarioAdminRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        if (usuario.Username != request.Username && await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
            return BadRequest("El nombre de usuario ya está en uso.");

        usuario.Username = request.Username;
        usuario.NombreCompleto = request.NombreCompleto;
        usuario.Email = request.Email;
        usuario.RolId = request.RolId;
        usuario.Activo = request.Activo;

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario actualizado correctamente." });
    }

    // 8. ELIMINAR USUARIO (Borrado Lógico)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EliminarUsuario(Guid id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        
        if (currentUserId == id.ToString())
            return BadRequest("No puedes eliminar tu propio usuario.");

        usuario.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario eliminado lógicamente." });
    }
}