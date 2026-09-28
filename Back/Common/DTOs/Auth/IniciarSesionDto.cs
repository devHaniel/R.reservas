using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Auth;

public class IniciarSesionDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}