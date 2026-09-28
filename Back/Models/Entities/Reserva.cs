using System.ComponentModel.DataAnnotations;

namespace Reservas.Models.Entities;

/// <summary>
/// Una reserva es un hecho inmutable desde el punto de vista contable:
/// guarda un "snapshot" del precio, la duración y el recurso al momento de crearse,
/// de modo que cambios posteriores en el catálogo no alteren el historial.
/// </summary>
public class Reserva
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int TipoServicioId { get; set; }
    public TipoServicio TipoServicio { get; set; } = null!;

    /// <summary>
    /// Recurso sobre el que se reserva. Se denormaliza (snapshot) porque:
    /// 1) permite la exclusion constraint que impide doble reserva, y
    /// 2) preserva el recurso histórico aunque el servicio cambie de recurso.
    /// </summary>
    public int RecursoReservableId { get; set; }
    public RecursoReservable RecursoReservable { get; set; } = null!;

    /// <summary>Hora local de pared (sin zona) del negocio.</summary>
    public DateTime FechaHoraInicio { get; set; }

    /// <summary>Hora local de pared (sin zona) del negocio.</summary>
    public DateTime FechaHoraFin { get; set; }

    public EstadoReserva Estado { get; set; }

    /// <summary>Instante UTC de auditoría.</summary>
    public DateTime FechaCreacion { get; set; }

    // --- Snapshot inmutable del servicio al momento de reservar ---

    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
    public string NombreServicio { get; set; } = string.Empty;

    /// <summary>Nota operativa opcional.</summary>
    [StringLength(300)]
    public string? Nota { get; set; }
}

public enum EstadoReserva
{
    Pendiente = 0,
    Confirmada = 1,
    Cancelada = 2,
    Completada = 3
}