using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.TipoServicio;

public class TipoServicioCrearDto
{
    [Range(1, int.MaxValue)]
    public int RecursoReservableId { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int DuracionMinutos { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal Precio { get; set; }
}
