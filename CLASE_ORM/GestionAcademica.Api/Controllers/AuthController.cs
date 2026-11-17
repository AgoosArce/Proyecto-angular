using GestionAcademica.Api.Configuration;
using GestionAcademica.Application.DTOs.Auth;
using GestionAcademica.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)] // límite por IP contra fuerza bruta
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Registra un usuario nuevo. Siempre con rol "Usuario".</summary>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto, CancellationToken ct) =>
        Ok(await _auth.RegisterAsync(dto, ct));

    /// <summary>Devuelve un JWT con el claim "role" leído de la base.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(dto, ct));
}
