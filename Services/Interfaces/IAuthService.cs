using Reservas.Common.DTOs.Auth;

namespace Reservas.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Succeeded, int? UserId, IEnumerable<string> Errors)> RegisterAsync(RegistroUsuarioDto dto);
}