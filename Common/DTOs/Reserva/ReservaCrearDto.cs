using System.ComponentModel.DataAnnotations;
using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Reserva;

public class ReservaCrearDto
{
    [Range(1, int.MaxValue)]
    public int ClienteId { get; set; }

    [Range(1, int.MaxValue)]
    public int RecursoReservableId { get; set; }

    [Range(1, int.MaxValue)]
    public int TipoServicioId { get; set; }

    [Required]
    public DateTime FechaHoraInicio { get; set; }

    [Required]
    public DateTime FechaHoraFin { get; set; }

    [EnumDataType(typeof(EstadoReserva))]
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
}
