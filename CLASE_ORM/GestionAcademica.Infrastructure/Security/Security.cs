using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionAcademica.Infrastructure.Security;

/// <summary>Sección "Jwt" de la configuración. La Key NO debe estar en appsettings.json.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
}

/// <summary>Nombres de claims cortos usados en el token (sin el mapeo a URIs largas de WS-Fed).</summary>
public static class JwtClaimNames
{
    public const string Role = "role";
    public const string Nombre = "nombre";
}

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash corrupto o en otro formato: se trata como contraseña incorrecta.
            return false;
        }
    }
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> options) => _settings = options.Value;

    public (string Token, DateTime Expiration) Generate(Usuario usuario)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtClaimNames.Nombre, usuario.Nombre),
            // El rol sale de la base de datos, nunca de lo que envía el cliente.
            new Claim(JwtClaimNames.Role, usuario.Rol)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
