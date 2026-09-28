namespace Reservas.Common.DTOs.TipoServicio;

public class TipoServicioDto
{
    public int Id { get; set; }
    public int RecursoReservableId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
}