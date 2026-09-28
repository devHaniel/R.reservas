using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservas.Models.Entities;

namespace Reservas.Data.Configurations;

/// <summary>
/// Los clientes son globales (una sola sede): cualquier usuario con rol Admin o
/// Vendedor los ve. El email/teléfono son únicos, pero solo entre clientes activos
/// (índice parcial), para permitir volver a registrar uno dado de baja.
/// </summary>
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        // Único solo entre filas no eliminadas.
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL AND \"EliminadoEn\" IS NULL");

        builder.HasIndex(c => c.Telefono)
            .IsUnique()
            .HasFilter("\"EliminadoEn\" IS NULL");

        builder.HasIndex(c => c.EliminadoEn);

        // No restringimos la visibilidad por vendedor; el campo es informativo.
        builder.HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}