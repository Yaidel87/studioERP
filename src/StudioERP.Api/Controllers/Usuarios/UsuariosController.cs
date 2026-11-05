using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ObtenerTodos()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Contactos.Where(contacto => !contacto.IsDeleted))
            .Where(u => !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.NombreCompleto,
                u.CarnetIdentidad,
                u.Email,
                u.PhotoUrl,
                u.Activo,
                Rol = u.Rol.Nombre,
                Contactos = u.Contactos.Where(contacto => !contacto.IsDeleted)
                    .Select(contacto => new { contacto.Id, contacto.Tipo, contacto.Valor })
                    .ToList()
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    // 2. CREAR USUARIO (Con automatización de Staff y múltiples especialidades)
    public record ContactoRequest(string Tipo, string Valor);
    public record CrearUsuarioRequest(
        string? Username,
        string? NombreCompleto,
        string? CarnetIdentidad,
        string? Email,
        string? Password,
        Guid RolId,
        bool Activo,
        List<ContactoRequest>? Contactos = null);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request)
    {
        var username = request.Username?.Trim();
        var nombreCompleto = request.NombreCompleto?.Trim();
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nombreCompleto) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Usuario, nombre completo, email y contraseña son obligatorios.");
        if (!new EmailAddressAttribute().IsValid(email))
            return BadRequest("El email no es válido.");
        var errorContactos = ValidarContactos(request.Contactos);
        if (errorContactos != null) return BadRequest(errorContactos);
        if (request.Password.Length < 6)
            return BadRequest("La contraseña debe tener al menos 6 caracteres.");
        if (request.RolId == Guid.Empty || !await _context.Roles.AnyAsync(r => r.Id == request.RolId && !r.IsDeleted))
            return BadRequest("Debe seleccionar un rol válido.");

        if (await _context.Usuarios.AnyAsync(u => u.Username == username))
            return BadRequest("El nombre de usuario ya existe.");

        var usuario = new Usuario
        {
            Username = username,
            NombreCompleto = nombreCompleto,
            CarnetIdentidad = request.CarnetIdentidad?.Trim(),
            Email = email,
            RolId = request.RolId,
            Activo = request.Activo
        };
        AgregarContactos(usuario, request.Contactos, email);

        usuario.PasswordHash = _authService.HashPassword(usuario, request.Password);
        
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // --- LÓGICA AUTOMÁTICA PARA STAFF ---
        if (usuario.Activo)
        {
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == usuario.RolId && !r.IsDeleted);
            
            if (rol != null && (rol.Nombre == "Fotógrafo" || rol.Nombre == "Maquillador" || rol.Nombre == "Editor"))
            {
                var tipoStaff = await _context.TiposStaff.FirstOrDefaultAsync(t => t.Nombre == rol.Nombre && !t.IsDeleted);
                
                if (tipoStaff == null)
                {
                    tipoStaff = new TipoStaff { Nombre = rol.Nombre };
                    _context.TiposStaff.Add(tipoStaff);
                    await _context.SaveChangesAsync();
                }

                // Se crea el staff y se le asigna la especialidad
                var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.UsuarioId == usuario.Id && !s.IsDeleted)
                    ?? new Staff { UsuarioId = usuario.Id };
                
                // AQUÍ ESTÁ EL CAMBIO CLAVE: Añadir a la lista en lugar de asignar un ID
                staff.TiposStaff.Add(tipoStaff);

                if (staff.Id == Guid.Empty || _context.Entry(staff).State == EntityState.Detached)
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
        if (!EsPropietarioOAdmin(id)) return Forbid();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ObtenerRoles()
    {
        var roles = await _context.Roles
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Id, r.Nombre })
            .ToListAsync();
        return Ok(roles);
    }

    // 5. CAMBIAR CONTRASEÑA (Usuario logueado)
    public record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);

    [HttpPut("password")]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request)
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.PasswordActual) || string.IsNullOrWhiteSpace(request.PasswordNueva) ||
            request.PasswordNueva.Length < 6)
            return BadRequest("La contraseña nueva debe tener al menos 6 caracteres.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.Activo);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        if (!_authService.VerifyPassword(usuario, request.PasswordActual))
            return BadRequest("La contraseña actual es incorrecta.");

        usuario.PasswordHash = _authService.HashPassword(usuario, request.PasswordNueva);
        
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contraseña actualizada correctamente." });
    }

    public record RestablecerPasswordRequest(string? PasswordNueva);

    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestablecerPassword(Guid id, RestablecerPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PasswordNueva) || request.PasswordNueva.Length < 6)
            return BadRequest("La contraseña debe tener al menos 6 caracteres.");

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        usuario.PasswordHash = _authService.HashPassword(usuario, request.PasswordNueva);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Contraseña restablecida correctamente." });
    }

    // 6. ACTUALIZAR PERFIL (Solo el propio usuario)
    public record ActualizarPerfilRequest(string NombreCompleto, string Email);

    [HttpPut("{id}/perfil")]
    public async Task<IActionResult> ActualizarPerfil(Guid id, ActualizarPerfilRequest request)
    {
        if (!EsPropietario(id)) return Forbid();
        var nombreCompleto = request.NombreCompleto?.Trim();
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(email))
            return BadRequest("El nombre completo y el email son obligatorios.");
        if (!new EmailAddressAttribute().IsValid(email))
            return BadRequest("El email no es válido.");

        var usuario = await _context.Usuarios
            .Include(u => u.Contactos)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted && u.Activo);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        usuario.NombreCompleto = nombreCompleto;
        usuario.Email = email;
        ReemplazarContactoEmail(usuario, email);

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Perfil actualizado correctamente." });
    }

    // 7. EDITAR USUARIO (ADMIN - Todo completo)
    public record EditarUsuarioAdminRequest(
        string? Username,
        string? NombreCompleto,
        string? CarnetIdentidad,
        string? Email,
        Guid RolId,
        bool Activo,
        List<ContactoRequest>? Contactos = null);

    [HttpPut("{id}/admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditarUsuarioAdmin(Guid id, EditarUsuarioAdminRequest request)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Contactos)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        var username = request.Username?.Trim();
        var nombreCompleto = request.NombreCompleto?.Trim();
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(email))
            return BadRequest("Usuario, nombre completo y email son obligatorios.");
        if (!new EmailAddressAttribute().IsValid(email))
            return BadRequest("El email no es válido.");
        var errorContactos = ValidarContactos(request.Contactos);
        if (errorContactos != null) return BadRequest(errorContactos);
        if (request.RolId == Guid.Empty || !await _context.Roles.AnyAsync(r => r.Id == request.RolId && !r.IsDeleted))
            return BadRequest("Debe seleccionar un rol válido.");

        if (usuario.Username != username && await _context.Usuarios.AnyAsync(u => u.Username == username))
            return BadRequest("El nombre de usuario ya está en uso.");

        usuario.Username = username;
        usuario.NombreCompleto = nombreCompleto;
        usuario.CarnetIdentidad = request.CarnetIdentidad?.Trim();
        usuario.Email = email;
        usuario.RolId = request.RolId;
        usuario.Activo = request.Activo;
        if (request.Contactos != null)
        {
            foreach (var contacto in usuario.Contactos.Where(contacto => !contacto.IsDeleted))
                contacto.IsDeleted = true;
            AgregarContactos(usuario, request.Contactos, email);
        }

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario actualizado correctamente." });
    }

    // 8. ELIMINAR USUARIO (Borrado Lógico)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EliminarUsuario(Guid id)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (usuario == null) return NotFound("Usuario no encontrado.");

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        
        if (currentUserId == id.ToString())
            return BadRequest("No puedes eliminar tu propio usuario.");

        usuario.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario eliminado lógicamente." });
    }

    private bool EsPropietario(Guid id)
    {
        var claimId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claimId, out var userId) && userId == id;
    }

    private bool EsPropietarioOAdmin(Guid id) => User.IsInRole("Admin") || EsPropietario(id);

    private static string? ValidarContactos(List<ContactoRequest>? contactos)
    {
        if (contactos == null) return null;
        foreach (var contacto in contactos)
        {
            if (contacto.Tipo is not ("Telefono" or "Email" or "Direccion") ||
                string.IsNullOrWhiteSpace(contacto.Valor))
                return "Cada contacto debe tener un tipo válido y un valor.";
            if (contacto.Tipo == "Email" && !new EmailAddressAttribute().IsValid(contacto.Valor))
                return $"El correo «{contacto.Valor}» no es válido.";
        }
        return null;
    }

    private static void AgregarContactos(Usuario usuario, List<ContactoRequest>? contactos, string emailPrincipal)
    {
        var lista = contactos?.ToList() ?? new List<ContactoRequest>();
        if (!lista.Any(contacto => contacto.Tipo == "Email" &&
                                  string.Equals(contacto.Valor, emailPrincipal, StringComparison.OrdinalIgnoreCase)))
            lista.Insert(0, new ContactoRequest("Email", emailPrincipal));

        foreach (var contacto in lista)
            usuario.Contactos.Add(new Contacto { Tipo = contacto.Tipo, Valor = contacto.Valor.Trim() });
    }

    private static void ReemplazarContactoEmail(Usuario usuario, string email)
    {
        foreach (var contacto in usuario.Contactos.Where(contacto => !contacto.IsDeleted && contacto.Tipo == "Email"))
            contacto.IsDeleted = true;
        AgregarContactos(usuario, null, email);
    }
}