using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservas.Models.Entities;

namespace Reservas.Data.Configurations;

/// <summary>
/// Las fechas de una reserva representan hora local de pared (la misma que los
/// horarios de apertura/cierre del recurso), por lo que se guardan como
/// "timestamp without time zone". Así las comparaciones de solapamiento en SQL
/// son exactas y no dependen de la zona horaria del servidor ni del cliente.
///
/// La garantía de "una sola reserva activa por turno" NO se confía al código:
/// se crea una exclusion constraint de PostgreSQL en la migración
/// (ver Migrations/*_LineaBase.cs), que impide el doble agendamiento incluso
/// con peticiones concurrentes.
/// </summary>
public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        // Hora local de pared: sin zona.
        builder.Property(r => r.FechaHoraInicio)
            .HasColumnType("timestamp without time zone");

        builder.Property(r => r.FechaHoraFin)
            .HasColumnType("timestamp without time zone");

        // Instante UTC de auditoría: se guarda CON zona (conserva el Kind=UTC).
        builder.Property(r => r.FechaCreacion)
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.Precio)
            .HasPrecision(12, 2);

        builder.Property(r => r.NombreServicio)
            .HasMaxLength(100)
            .IsRequired();

        // Las FK a catálogo son Restrict: nunca se borra físicamente un recurso,
        // servicio o cliente con reservas; se usa borrado lógico.
        builder.HasOne(r => r.Cliente)
            .WithMany(c => c.Reservas)
            .HasForeignKey(r => r.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TipoServicio)
            .WithMany()
            .HasForeignKey(r => r.TipoServicioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RecursoReservable)
            .WithMany()
            .HasForeignKey(r => r.RecursoReservableId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para las consultas típicas: calendario por recurso y por cliente.
        builder.HasIndex(r => new { r.RecursoReservableId, r.FechaHoraInicio });
        builder.HasIndex(r => new { r.ClienteId, r.FechaHoraInicio });
        builder.HasIndex(r => r.FechaHoraInicio);
    }
}