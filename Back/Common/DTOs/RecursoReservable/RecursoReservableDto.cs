namespace Reservas.Common.DTOs.RecursoReservable;

public class RecursoReservableDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoDeporte { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public TimeSpan HorarioAperturaDefault { get; set; }
    public TimeSpan HorarioCierreDefault { get; set; }
}
