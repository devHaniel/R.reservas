using System;
using System.Security.Claims;
using Reservas.Models.Entities;

namespace Reservas.Services.Interfaces;

public interface ITokenService
{
    string GenerarToken(int idUsuario);
    string GenerarRefreshToken();
    Task<HistorialRefreshToken> GuardarHistorialRefreshToken(int idUsuario, string token, string refreshToken);
    Task<HistorialRefreshToken?> DevolverRefreshToken(string refreshToken);
    Task<bool> RevocarRefreshTokenAsync(HistorialRefreshToken historial);
    ClaimsPrincipal? ObtenerClaimsDesdeTokenExpirado(string token);
}
