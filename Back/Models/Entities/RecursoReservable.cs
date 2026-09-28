using System.ComponentModel.DataAnnotations;

namespace Reservas.Models.Entities;

public class RecursoReservable
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;              // "Cancha 1", "Cancha techada 3"

    [Required]
    [StringLength(60)]
    public string TipoDeporte { get; set; } = string.Empty;          // "Fútbol 5", "Pádel", "Tenis"

    /// <summary>Permite deshabilitar temporalmente el recurso sin borrarlo.</summary>
    public bool Activo { get; set; } = true;

    public TimeSpan HorarioAperturaDefault { get; set; }  // ej: 08:00
    public TimeSpan HorarioCierreDefault { get; set; }    // ej: 23:00

    /// <summary>Borrado lógico: la fila nunca se elimina para preservar el historial.</summary>
    public DateTime? EliminadoEn { get; set; }

    public ICollection<TipoServicio> ServiciosDisponibles { get; set; } = new List<TipoServicio>();
}