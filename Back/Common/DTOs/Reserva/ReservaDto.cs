using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Reserva;

public class ReservaDto
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public string NombreCliente { get; set; } = string.Empty;

    public int RecursoReservableId { get; set; }
    public string NombreRecurso { get; set; } = string.Empty;

    public int TipoServicioId { get; set; }
    public string NombreServicio { get; set; } = string.Empty;

    /// <summary>Hora local del negocio. Formato "yyyy-MM-ddTHH:mm".</summary>
    public string FechaHoraInicio { get; set; } = string.Empty;

    /// <summary>Hora local del negocio. Formato "yyyy-MM-ddTHH:mm".</summary>
    public string FechaHoraFin { get; set; } = string.Empty;

    public EstadoReserva Estado { get; set; }

    /// <summary>Precio acordado al reservar (snapshot inmutable).</summary>
    public decimal Precio { get; set; }

    /// <summary>Duración acordada al reservar (snapshot inmutable).</summary>
    public int DuracionMinutos { get; set; }

    /// <summary>Nota operativa opcional.</summary>
    public string? Nota { get; set; }

    /// <summary>Fecha de creación. Formato "yyyy-MM-ddTHH:mmZ" (UTC).</summary>
    public string FechaCreacion { get; set; } = string.Empty;
}