using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservas.Models.Entities;

namespace Reservas.Data.Configurations;

public class RecursoReservableConfiguration : IEntityTypeConfiguration<RecursoReservable>
{
    public void Configure(EntityTypeBuilder<RecursoReservable> builder)
    {
        builder.HasIndex(r => r.EliminadoEn);

        builder.Property(r => r.HorarioAperturaDefault)
            .HasColumnType("interval");

        builder.Property(r => r.HorarioCierreDefault)
            .HasColumnType("interval");
    }
}