using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Auth;

public class RegistroUsuarioDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}