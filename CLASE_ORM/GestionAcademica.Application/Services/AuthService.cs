using GestionAcademica.Application.DTOs.Auth;
using GestionAcademica.Application.Exceptions;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Constants;
using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _tokens;

    public AuthService(IUsuarioRepository usuarios, IPasswordHasher hasher, IJwtTokenGenerator tokens)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var usuario = await _usuarios.GetByEmailAsync(NormalizarEmail(dto.Email), ct);

        // Mismo mensaje para "no existe" y "contraseña incorrecta": no revela qué emails están registrados.
        if (usuario is null || !_hasher.Verify(dto.Password, usuario.PasswordHash))
            throw new UnauthorizedException("INVALID_CREDENTIALS", "Email o contraseña inválidos.");

        return CrearRespuesta(usuario);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var email = NormalizarEmail(dto.Email);
        if (await _usuarios.EmailExistsAsync(email, ct))
            throw new ConflictException("EMAIL_DUPLICADO", "Ya existe un usuario con ese email.");

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            Rol = Roles.Usuario // nunca Admin desde el registro público
        };

        await _usuarios.AddAsync(usuario, ct);
        await _usuarios.SaveChangesAsync(ct);

        return CrearRespuesta(usuario);
    }

    private AuthResponseDto CrearRespuesta(Usuario usuario)
    {
        var (token, expiration) = _tokens.Generate(usuario);
        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol
        };
    }

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
