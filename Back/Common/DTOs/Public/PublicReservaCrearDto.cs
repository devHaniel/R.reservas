using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Public;

public class PublicReservaCrearDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [StringLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TipoServicioId { get; set; }

    /// <summary>Hora local del negocio, formato "yyyy-MM-ddTHH:mm".</summary>
    [Required]
    public string FechaHoraInicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verifica tu correo antes de confirmar la reserva.")]
    public string TokenVerificacion { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Nota { get; set; }
}
