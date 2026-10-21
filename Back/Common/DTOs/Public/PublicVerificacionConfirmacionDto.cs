using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Public;

public class PublicVerificacionConfirmacionDto
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código es obligatorio.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;
}

public class PublicVerificacionConfirmadaDto
{
    public string Token { get; set; } = string.Empty;
    public int ExpiraEnSegundos { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
