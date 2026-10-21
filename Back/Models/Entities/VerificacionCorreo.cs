using System.ComponentModel.DataAnnotations;

namespace Reservas.Models.Entities;

/// <summary>
/// Verificación de correo para reservas públicas. Se guardan hashes, nunca el
/// código ni el token en texto plano. Cada fila sirve para un correo y puede
/// emitir un token de un solo uso después de confirmar el código.
/// </summary>
public class VerificacionCorreo
{
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string CodigoHash { get; set; } = string.Empty;

    public DateTime CodigoExpiraEn { get; set; }

    public DateTime? CodigoUsadoEn { get; set; }

    public int Intentos { get; set; }

    [StringLength(128)]
    public string? TokenHash { get; set; }

    public DateTime? TokenExpiraEn { get; set; }

    public DateTime? TokenUsadoEn { get; set; }

    public DateTime CreadoEn { get; set; }
}
