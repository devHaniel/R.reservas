using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Public;

public class PublicVerificacionSolicitudDto
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

public class PublicVerificacionSolicitadaDto
{
    public string Mensaje { get; set; } = string.Empty;
    public int ExpiraEnSegundos { get; set; }
    public int ReenviarEnSegundos { get; set; }

    /// <summary>Solo se informa en Desarrollo cuando SMTP no está configurado.</summary>
    public string? CodigoDesarrollo { get; set; }
}
