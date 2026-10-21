using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservas.Models.Entities;

namespace Reservas.Data.Configurations;

public class VerificacionCorreoConfiguration : IEntityTypeConfiguration<VerificacionCorreo>
{
    public void Configure(EntityTypeBuilder<VerificacionCorreo> builder)
    {
        builder.Property(v => v.Email).HasMaxLength(256).IsRequired();
        builder.Property(v => v.CodigoHash).HasMaxLength(128).IsRequired();
        builder.Property(v => v.TokenHash).HasMaxLength(128);

        builder.HasIndex(v => v.Email);
        builder.HasIndex(v => new { v.Email, v.CreadoEn });
        builder.HasIndex(v => v.TokenHash)
            .IsUnique()
            .HasFilter("\"TokenHash\" IS NOT NULL");

        // Las fechas son instantes UTC (auditoría y vencimiento).
        builder.Property(v => v.CodigoExpiraEn).HasColumnType("timestamp with time zone");
        builder.Property(v => v.CodigoUsadoEn).HasColumnType("timestamp with time zone");
        builder.Property(v => v.TokenExpiraEn).HasColumnType("timestamp with time zone");
        builder.Property(v => v.TokenUsadoEn).HasColumnType("timestamp with time zone");
        builder.Property(v => v.CreadoEn).HasColumnType("timestamp with time zone");
    }
}
