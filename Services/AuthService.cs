using Microsoft.AspNetCore.Identity;
using Reservas.Common.DTOs.Auth;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<Usuario> _userManager;

    public AuthService(UserManager<Usuario> userManager)
    {
        _userManager = userManager;
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
}