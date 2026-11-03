using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using StudioERP.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace StudioERP.Infrastructure.Services;

public class AuthService
{
    private readonly PasswordHasher<Usuario> _passwordHasher;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(IConfiguration config)
    {
        _passwordHasher = new PasswordHasher<Usuario>();
        _jwtKey = config["Jwt:Key"] ?? throw new Exception("Falta Jwt:Key en appsettings");
        _jwtIssuer = config["Jwt:Issuer"] ?? "StudioERP";
        _jwtAudience = config["Jwt:Audience"] ?? "StudioERP";
    }

    // 1. Encriptar contraseña
    public string HashPassword(Usuario usuario, string password)
    {
        return _passwordHasher.HashPassword(usuario, password);
    }

    // 2. Verificar contraseña en el Login
    public bool VerifyPassword(Usuario usuario, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        return result == PasswordVerificationResult.Success;
    }

    // 3. Generar el Token JWT
    public string GenerateToken(Usuario usuario, string rolNombre)
    {
        // Definimos los Claims (datos) que irán dentro del Token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),       // ID del usuario
            new Claim("nickname", usuario.Username),                             // Nickname (para la UI)
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),                  // Nombre completo
            new Claim(ClaimTypes.Role, rolNombre)                                // ROL (VITAL para IsInRole)
        };

        // Creamos la clave de seguridad
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Creamos el Token con expiración de 8 horas
        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}