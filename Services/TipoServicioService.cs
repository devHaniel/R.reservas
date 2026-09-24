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
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        return tipos.Select(t => new TipoServicioDto
        {
            Id = t.Id,
            RecursoReservableId = t.RecursoReservableId,
            Nombre = t.Nombre,
            DuracionMinutos = t.DuracionMinutos,
            Precio = t.Precio
        }).ToList();
    }

    public async Task<TipoServicioDto?> GetByIdAsync(int id)
    {
        var tipo = await _context.TiposServicio
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tipo is null)
            return null;

        return new TipoServicioDto
        {
            Id = tipo.Id,
            RecursoReservableId = tipo.RecursoReservableId,
            Nombre = tipo.Nombre,
            DuracionMinutos = tipo.DuracionMinutos,
            Precio = tipo.Precio
        };
    }

    public async Task<List<TipoServicioDto>> GetByRecursoAsync(int recursoReservableId)
    {
        var tipos = await _context.TiposServicio
            .AsNoTracking()
            .Where(t => t.RecursoReservableId == recursoReservableId)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        return tipos.Select(t => new TipoServicioDto
        {
            Id = t.Id,
            RecursoReservableId = t.RecursoReservableId,
            Nombre = t.Nombre,
            DuracionMinutos = t.DuracionMinutos,
            Precio = t.Precio
        }).ToList();
    }

    public async Task<TipoServicioDto> CreateAsync(TipoServicioCrearDto dto)
    {
        var tipo = new TipoServicio
        {
            RecursoReservableId = dto.RecursoReservableId,
            Nombre = dto.Nombre,
            DuracionMinutos = dto.DuracionMinutos,
            Precio = dto.Precio
        };

        _context.TiposServicio.Add(tipo);
        await _context.SaveChangesAsync();

        return new TipoServicioDto
        {
            Id = tipo.Id,
            RecursoReservableId = tipo.RecursoReservableId,
            Nombre = tipo.Nombre,
            DuracionMinutos = tipo.DuracionMinutos,
            Precio = tipo.Precio
        };
    }

    public async Task<TipoServicioDto> UpdateAsync(TipoServicioActualizarDto dto)
    {
        var tipo = await _context.TiposServicio
            .FirstOrDefaultAsync(t => t.Id == dto.Id);

        if (tipo is null)
            throw new KeyNotFoundException("Tipo de servicio no encontrado");

        tipo.RecursoReservableId = dto.RecursoReservableId;
        tipo.Nombre = dto.Nombre;
        tipo.DuracionMinutos = dto.DuracionMinutos;
        tipo.Precio = dto.Precio;

        await _context.SaveChangesAsync();

        return new TipoServicioDto
        {
            Id = tipo.Id,
            RecursoReservableId = tipo.RecursoReservableId,
            Nombre = tipo.Nombre,
            DuracionMinutos = tipo.DuracionMinutos,
            Precio = tipo.Precio
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tipo = await _context.TiposServicio
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tipo is null)
            return false;

        _context.TiposServicio.Remove(tipo);
        await _context.SaveChangesAsync();

        return true;
    }
}
