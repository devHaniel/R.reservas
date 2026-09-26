using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.Auth;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[ApiController]
[Route("api/auth")]
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
        var token = await _authService.LoginAsync(dto);
        if (token is null)
            return Unauthorized(new { mensaje = "Email o contraseña incorrectos" });

        return Ok(new { accessToken = token });
    }
}