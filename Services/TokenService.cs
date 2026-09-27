using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    public TokenService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public string GenerarToken(int idUsuario, IEnumerable<string> roles)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer no configurado");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience no configurado");
        var accessTokenMinutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes");
        var keyBytes = Encoding.UTF8.GetBytes(key);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, idUsuario.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credencialesToken = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256Signature
        );

        // detalle del token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            SigningCredentials = credencialesToken,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenConfiguracion = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(tokenConfiguracion);
    }

    public string GenerarRefreshToken()
    {
        var byteArray = new byte[64];
        var refreshToken = "";

        using(var rng = RandomNumberGenerator.Create()){
            rng.GetBytes(byteArray);
            refreshToken = Convert.ToBase64String(byteArray);
        }

        return refreshToken;
    }

    public async Task<HistorialRefreshToken> GuardarHistorialRefreshToken(
        int idUsuario,
        string token,
        string refreshToken
    )
    {
        var refreshTokenDays = _configuration.GetValue<int>("Jwt:RefreshTokenDays");
        if (refreshTokenDays <= 0)
            throw new InvalidOperationException("Jwt:RefreshTokenDays debe ser mayor que cero");

        var fechaCreacion = DateTime.UtcNow;
        var historialRefresh = new HistorialRefreshToken
        {
            UsuarioId = idUsuario,
            Token = token,
            RefreshToken = HashRefreshToken(refreshToken),
            FechaCreacion = fechaCreacion,
            FechaExpiracion = fechaCreacion.AddDays(refreshTokenDays)
        };

        await _context.HistorialRefreshTokens.AddAsync(historialRefresh);
        await _context.SaveChangesAsync();

        return historialRefresh;
    }

    public Task<HistorialRefreshToken?> DevolverRefreshToken(string refreshToken)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);
        return _context.HistorialRefreshTokens
            .AsNoTracking()
            .Where(historial => historial.RefreshToken == refreshTokenHash && historial.FechaExpiracion > DateTime.UtcNow)
            .OrderByDescending(historial => historial.FechaCreacion)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> RevocarRefreshTokenAsync(HistorialRefreshToken historial)
    {
        var now = DateTime.UtcNow;
        var updatedRows = await _context.HistorialRefreshTokens
            .Where(token => token.Id == historial.Id
                && token.RefreshToken == historial.RefreshToken
                && token.FechaExpiracion > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(token => token.FechaExpiracion, now));

        return updatedRows == 1;
    }

    public ClaimsPrincipal? ObtenerClaimsDesdeTokenExpirado(string token)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer no configurado");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience no configurado");

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        return Convert.ToBase64String(SHA256.HashData(tokenBytes));
    }
}
