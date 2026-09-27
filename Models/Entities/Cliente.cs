using System.ComponentModel.DataAnnotations;

namespace Reservas.Models.Entities;

public class Cliente
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }
    
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}