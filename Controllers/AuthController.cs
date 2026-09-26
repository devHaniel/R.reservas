using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reservas.Common.DTOs.Auth;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("fixed-policy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistroUsuarioDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = result.UserId,
            mensaje = "Usuario creado correctamente"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(IniciarSesionDto dto)
    {
        var tokens = await _authService.LoginAsync(dto);
        if (tokens is null)
            return Unauthorized(new { mensaje = "Email o contraseña incorrectos" });

        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
    {
        var tokens = await _authService.RefreshTokenAsync(dto);
        if (tokens is null)
            return Unauthorized();

        return Ok(tokens);
    }
}