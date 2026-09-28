using System.ComponentModel.DataAnnotations;
using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Reserva;

public class ReservaActualizarDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int ClienteId { get; set; }

    [Range(1, int.MaxValue)]
    public int TipoServicioId { get; set; }

    /// <summary>
    /// Fecha y hora de inicio en hora local del negocio, formato "yyyy-MM-ddTHH:mm".
    /// Si cambia, el backend recalcula fin, duración y precio (nuevo snapshot).
    /// </summary>
    [Required]
    public string FechaHoraInicio { get; set; } = string.Empty;

    [EnumDataType(typeof(EstadoReserva))]
    public EstadoReserva Estado { get; set; }

    [StringLength(300)]
    public string? Nota { get; set; }
}