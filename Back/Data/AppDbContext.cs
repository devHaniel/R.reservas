using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Reservas.Models.Entities;

namespace Reservas.Data;

public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HistorialRefreshToken> HistorialRefreshTokens {get; set;}
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<RecursoReservable> RecursosReservables { get; set; }
    public DbSet<TipoServicio> TiposServicio { get; set; }
    public DbSet<Reserva> Reservas { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);  // importante: crea las tablas de Identity

        builder.Entity<Usuario>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.Entity<Usuario>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        // Todas las configuraciones de entidades se declaran aquí.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}