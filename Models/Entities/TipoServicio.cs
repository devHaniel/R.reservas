using System.ComponentModel.DataAnnotations;

namespace Reservas.Models.Entities;

public class TipoServicio
{
    public int Id { get; set; }
    public int RecursoReservableId { get; set; }
    public RecursoReservable RecursoReservable { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;        // "Turno 1 hora"

    [Range(1, 1440)]
    public int DuracionMinutos { get; set; }  // 60, 90

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal Precio { get; set; }
}