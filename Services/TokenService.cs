using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerarToken(int idUsuario)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer no configurado");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience no configurado");
        var accessTokenMinutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes");
        var keyBytes = Encoding.UTF8.GetBytes(key);

        var claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()));

        var credencialesToken = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256Signature
        );

        // detalle del token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            SigningCredentials = credencialesToken,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenConfiguracion = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(tokenConfiguracion);
    }
}
