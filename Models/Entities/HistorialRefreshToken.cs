using System;

namespace Reservas.Models.Entities;

public class HistorialRefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public byte EsActivo => (byte)(FechaExpiracion > DateTime.UtcNow ? 1 : 0);

    public Usuario Usuario { get; set; } = null!;
}
