using Reservas.Models.Entities;

namespace Reservas.Common.DTOs.Public;

public class PublicReservaDto
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string NombreRecurso { get; set; } = string.Empty;
    public string NombreServicio { get; set; } = string.Empty;
    public string FechaHoraInicio { get; set; } = string.Empty;
    public string FechaHoraFin { get; set; } = string.Empty;
    public EstadoReserva Estado { get; set; }
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
