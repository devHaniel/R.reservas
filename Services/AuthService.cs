using Microsoft.AspNetCore.Identity;
using Reservas.Common.DTOs.Auth;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<Usuario> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<(bool Succeeded, int? UserId, IEnumerable<string> Errors)> RegisterAsync(RegistroUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = dto.Email.Trim(),
            UserName = dto.Email.Trim()
        };

        var result = await _userManager.CreateAsync(usuario, dto.Password);

        if (!result.Succeeded)
            return (false, null, result.Errors.Select(error => error.Description));

        return (true, usuario.Id, []);
    }

    public async Task<string?> LoginAsync(IniciarSesionDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (usuario is null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
            return null;

        return _tokenService.GenerarToken(usuario.Id);
    }
}