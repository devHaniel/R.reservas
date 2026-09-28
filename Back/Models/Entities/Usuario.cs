using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Reservas.Models.Entities;

public class Usuario : IdentityUser<int>   // <int> para que el Id sea int, no Guid
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
}