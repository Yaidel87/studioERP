using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioERP.Domain.Entities;
using StudioERP.Infrastructure.Data;
using StudioERP.Infrastructure.Services;

namespace StudioERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly StudioDbContext _context;
    private readonly AuthService _authService;

    public AuthController(StudioDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // DTOs (Data Transfer Objects) para recibir datos del Frontend
    public record RegistroRequest(string Username, string NombreCompleto, string Email, string Password);
    public record LoginRequest(string Username, string Password);

    [HttpPost("registro")]
    public async Task<IActionResult> Registro(RegistroRequest request)
    {
        // 1. Verificar si el usuario ya existe
        if (await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
            return BadRequest("El nombre de usuario ya está en uso.");

        // 2. Buscar o crear el Rol por defecto
        var rolAdmin = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Admin");
        if (rolAdmin == null)
        {
            rolAdmin = new Rol { Nombre = "Admin" };
            _context.Roles.Add(rolAdmin);
            await _context.SaveChangesAsync(); // Guardamos para obtener el ID del rol
        }

        // 3. Crear el nuevo Usuario
        var nuevoUsuario = new Usuario
        {
            Username = request.Username,
            NombreCompleto = request.NombreCompleto,
            Email = request.Email,
            RolId = rolAdmin.Id,
            Activo = true
        };

        // 4. Encriptar contraseña y guardar
        nuevoUsuario.PasswordHash = _authService.HashPassword(nuevoUsuario, request.Password);
        
        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario registrado con éxito", id = nuevoUsuario.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // 1. Buscar al usuario con su Rol
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (usuario == null || !usuario.Activo)
            return Unauthorized("Credenciales inválidas o usuario inactivo.");

        // 2. Verificar la contraseña
        if (!_authService.VerifyPassword(usuario, request.Password))
            return Unauthorized("Credenciales inválidas.");

        // 3. Generar el Token
        var token = _authService.GenerateToken(usuario, usuario.Rol.Nombre);

        // 4. Devolver el Token al Frontend
        return Ok(new
        {
            token = token,
            username = usuario.Username,
            nombre = usuario.NombreCompleto,
            rol = usuario.Rol.Nombre
        });
    }
}