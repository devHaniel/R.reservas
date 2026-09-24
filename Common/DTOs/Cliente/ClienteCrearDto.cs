using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Cliente;

public class ClienteCrearDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }
}
