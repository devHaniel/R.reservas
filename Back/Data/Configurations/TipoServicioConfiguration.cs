using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservas.Models.Entities;

namespace Reservas.Data.Configurations;

public class TipoServicioConfiguration : IEntityTypeConfiguration<TipoServicio>
{
    public void Configure(EntityTypeBuilder<TipoServicio> builder)
    {
        builder.HasIndex(t => t.EliminadoEn);

        // Restrict: no se puede borrar físicamente un recurso con servicios asociados.
        // El borrado lógico se hace marcando EliminadoEn.
        builder.HasOne(t => t.RecursoReservable)
            .WithMany(r => r.ServiciosDisponibles)
            .HasForeignKey(t => t.RecursoReservableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}