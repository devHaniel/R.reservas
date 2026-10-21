// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Reservas.Common.DTOs.Auth;

/// <summary>Alta de personal desde el panel de administración.</summary>
public class CrearUsuarioDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Admin o Vendedor. Si se omite, se crea como Vendedor.</summary>
    public string? Rol { get; set; }
}
