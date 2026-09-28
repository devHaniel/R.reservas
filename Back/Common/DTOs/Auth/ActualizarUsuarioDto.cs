using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Auth;

public class ActualizarUsuarioDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public string? Rol { get; set; }
}