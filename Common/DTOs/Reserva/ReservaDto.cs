using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Reserva;

public class ReservaDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int RecursoReservableId { get; set; }
    public int TipoServicioId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public EstadoReserva Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
}
