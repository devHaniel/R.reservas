using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.TipoServicio;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class TipoServicioService : ITipoServicioService
{
    private readonly AppDbContext _context;

    public TipoServicioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TipoServicioDto>> GetAllAsync()
    {
        var tipos = await _context.TiposServicio
            .AsNoTracking()
            .Where(t => t.EliminadoEn == null)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        return tipos.Select(Map).ToList();
    }

    public async Task<TipoServicioDto?> GetByIdAsync(int id)
    {
        var tipo = await _context.TiposServicio
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.EliminadoEn == null);

        return tipo is null ? null : Map(tipo);
    }

    public async Task<List<TipoServicioDto>> GetByRecursoAsync(int recursoReservableId)
    {
        var tipos = await _context.TiposServicio
            .AsNoTracking()
            .Where(t => t.EliminadoEn == null && t.RecursoReservableId == recursoReservableId)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        return tipos.Select(Map).ToList();
    }

    public async Task<TipoServicioDto> CreateAsync(TipoServicioCrearDto dto)
    {
        await AsegurarRecursoValidoAsync(dto.RecursoReservableId);

        var tipo = new TipoServicio
        {
            RecursoReservableId = dto.RecursoReservableId,
            Nombre = dto.Nombre,
            DuracionMinutos = dto.DuracionMinutos,
            Precio = dto.Precio,
            Activo = dto.Activo
        };

        _context.TiposServicio.Add(tipo);
        await _context.SaveChangesAsync();

        return Map(tipo);
    }

    public async Task<TipoServicioDto> UpdateAsync(TipoServicioActualizarDto dto)
    {
        await AsegurarRecursoValidoAsync(dto.RecursoReservableId);

        var tipo = await _context.TiposServicio
            .FirstOrDefaultAsync(t => t.Id == dto.Id && t.EliminadoEn == null)
            ?? throw new KeyNotFoundException("Tipo de servicio no encontrado");

        tipo.RecursoReservableId = dto.RecursoReservableId;
        tipo.Nombre = dto.Nombre;
        tipo.DuracionMinutos = dto.DuracionMinutos;
        tipo.Precio = dto.Precio;
        tipo.Activo = dto.Activo;

        await _context.SaveChangesAsync();

        return Map(tipo);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tipo = await _context.TiposServicio
            .FirstOrDefaultAsync(t => t.Id == id && t.EliminadoEn == null);

        if (tipo is null)
            return false;

        // Borrado lógico: conserva reservas históricas.
        tipo.EliminadoEn = DateTime.UtcNow;
        tipo.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task AsegurarRecursoValidoAsync(int recursoReservableId)
    {
        var existe = await _context.RecursosReservables
            .AnyAsync(r => r.Id == recursoReservableId && r.EliminadoEn == null);

        if (!existe)
            throw new KeyNotFoundException("Recurso reservable no encontrado");
    }

    private static TipoServicioDto Map(TipoServicio t) => new()
    {
        Id = t.Id,
        RecursoReservableId = t.RecursoReservableId,
        Nombre = t.Nombre,
        DuracionMinutos = t.DuracionMinutos,
        Precio = t.Precio,
        Activo = t.Activo
    };
}