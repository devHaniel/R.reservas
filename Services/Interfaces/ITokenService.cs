using System;

namespace Reservas.Services.Interfaces;

public interface ITokenService
{
    string GenerarToken(int idUsuario);
}
