using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Reservas.Common.DTOs.Auth;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<Usuario> userManager,
        ITokenService tokenService,
        AppDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
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

    public async Task<AuthTokensDto?> LoginAsync(IniciarSesionDto dto)
    {
        var usuario = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (usuario is null || !await _userManager.CheckPasswordAsync(usuario, dto.Password))
            return null;

        var accessToken = _tokenService.GenerarToken(usuario.Id);
        var refreshToken = _tokenService.GenerarRefreshToken();
        var historial = await _tokenService.GuardarHistorialRefreshToken(
            usuario.Id,
            accessToken,
            refreshToken);

        return new AuthTokensDto
        {
            AccessToken = historial.Token,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthTokensDto?> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var principal = _tokenService.ObtenerClaimsDesdeTokenExpirado(dto.AccessToken);
        if (principal is null)
            return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return null;

        var historial = await _tokenService.DevolverRefreshToken(dto.RefreshToken);
        if (historial is null || historial.UsuarioId != userId || historial.Token != dto.AccessToken)
            return null;

        var usuario = await _userManager.FindByIdAsync(userId.ToString());
        if (usuario is null)
            return null;

        var accessToken = _tokenService.GenerarToken(usuario.Id);
        var refreshToken = _tokenService.GenerarRefreshToken();
        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (!await _tokenService.RevocarRefreshTokenAsync(historial))
        {
            await transaction.RollbackAsync();
            return null;
        }

        var nuevoHistorial = await _tokenService.GuardarHistorialRefreshToken(
            usuario.Id,
            accessToken,
            refreshToken);
        await transaction.CommitAsync();

        return new AuthTokensDto
        {
            AccessToken = nuevoHistorial.Token,
            RefreshToken = refreshToken
        };
    }
}