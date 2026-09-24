using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.RecursoReservable;

public class RecursoReservableCrearDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string TipoDeporte { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    [Range(typeof(TimeSpan), "00:00:00", "23:59:59")]
    public TimeSpan HorarioAperturaDefault { get; set; }

    [Range(typeof(TimeSpan), "00:00:00", "23:59:59")]
    public TimeSpan HorarioCierreDefault { get; set; }
}
