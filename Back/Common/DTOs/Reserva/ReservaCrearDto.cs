using System.ComponentModel.DataAnnotations;
using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Reserva;

public class ReservaCrearDto
{
    [Range(1, int.MaxValue)]
    public int ClienteId { get; set; }

    [Range(1, int.MaxValue)]
    public int TipoServicioId { get; set; }

    /// <summary>
    /// Fecha y hora de inicio en hora local del negocio, formato "yyyy-MM-ddTHH:mm"
    /// (ej. "2026-10-10T16:00"). Sin zona horaria.
    /// El precio, la duración, el recurso y la hora de fin los calcula el backend.
    /// </summary>
    [Required]
    public string FechaHoraInicio { get; set; } = string.Empty;

    [EnumDataType(typeof(EstadoReserva))]
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

    /// <summary>Nota opcional del vendedor (ej. "cliente pidió pelota").</summary>
    [StringLength(300)]
    public string? Nota { get; set; }
}