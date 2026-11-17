using CLASE_ORM.Context;
using CLASE_ORM.Entities;
using CLASE_ORM.Entities.DTO.Auth;
using CLASE_ORM.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CLASE_ORM.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (usuario == null) return null;

            var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!isValid) return null;

            var token = GenerateToken(usuario);
            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
                Email = usuario.Email,
                Nombre = usuario.Nombre
            };
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
        {
            var existing = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
            if (existing) return null;

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var token = GenerateToken(usuario);
            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
                Email = usuario.Email,
                Nombre = usuario.Nombre
            };
        }

        private string GenerateToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("nombre", usuario.Nombre)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
