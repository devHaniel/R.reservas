using System.ComponentModel.DataAnnotations;

namespace Reservas.Models.Entities;

public class Reserva
{
    public int Id { get; set; }
    
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    
    public int TipoServicioId { get; set; }
    public TipoServicio TipoServicio { get; set; } = null!;
    
    [Required]
    public DateTime FechaHoraInicio { get; set; }

    [Required]
    public DateTime FechaHoraFin { get; set; }   // se calcula: Inicio + Duracion del servicio
    
    public EstadoReserva Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public enum EstadoReserva
{
    Pendiente,
    Confirmada,
    Cancelada,
    Completada
}