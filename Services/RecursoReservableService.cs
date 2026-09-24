using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.RecursoReservable;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class RecursoReservableService : IRecursoReservableService
{
    private readonly AppDbContext _context;

    public RecursoReservableService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecursoReservableDto>> GetAllAsync()
    {
        var recursos = await _context.RecursosReservables
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        return recursos.Select(r => new RecursoReservableDto
        {
            Id = r.Id,
            Nombre = r.Nombre,
            TipoDeporte = r.TipoDeporte,
            Activo = r.Activo,
            HorarioAperturaDefault = r.HorarioAperturaDefault,
            HorarioCierreDefault = r.HorarioCierreDefault
        }).ToList();
    }

    public async Task<RecursoReservableDto?> GetByIdAsync(int id)
    {
        var recurso = await _context.RecursosReservables
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recurso is null)
            return null;

        return new RecursoReservableDto
        {
            Id = recurso.Id,
            Nombre = recurso.Nombre,
            TipoDeporte = recurso.TipoDeporte,
            Activo = recurso.Activo,
            HorarioAperturaDefault = recurso.HorarioAperturaDefault,
            HorarioCierreDefault = recurso.HorarioCierreDefault
        };
    }

    public async Task<List<RecursoReservableDto>> GetByTipoDeporteAsync(string tipoDeporte)
    {
        var recursos = await _context.RecursosReservables
            .AsNoTracking()
            .Where(r => r.TipoDeporte.ToUpper().Contains(tipoDeporte.ToUpper()))
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        return recursos.Select(r => new RecursoReservableDto
        {
            Id = r.Id,
            Nombre = r.Nombre,
            TipoDeporte = r.TipoDeporte,
            Activo = r.Activo,
            HorarioAperturaDefault = r.HorarioAperturaDefault,
            HorarioCierreDefault = r.HorarioCierreDefault
        }).ToList();
    }

    public async Task<RecursoReservableDto> CreateAsync(RecursoReservableCrearDto dto)
    {
        var recurso = new RecursoReservable
        {
            Nombre = dto.Nombre,
            TipoDeporte = dto.TipoDeporte,
            Activo = dto.Activo,
            HorarioAperturaDefault = dto.HorarioAperturaDefault,
            HorarioCierreDefault = dto.HorarioCierreDefault
        };

        _context.RecursosReservables.Add(recurso);
        await _context.SaveChangesAsync();

        return new RecursoReservableDto
        {
            Id = recurso.Id,
            Nombre = recurso.Nombre,
            TipoDeporte = recurso.TipoDeporte,
            Activo = recurso.Activo,
            HorarioAperturaDefault = recurso.HorarioAperturaDefault,
            HorarioCierreDefault = recurso.HorarioCierreDefault
        };
    }

    public async Task<RecursoReservableDto> UpdateAsync(RecursoReservableActualizarDto dto)
    {
        var recurso = await _context.RecursosReservables
            .FirstOrDefaultAsync(r => r.Id == dto.Id);

        if (recurso is null)
            throw new KeyNotFoundException("Recurso reservable no encontrado");

        recurso.Nombre = dto.Nombre;
        recurso.TipoDeporte = dto.TipoDeporte;
        recurso.Activo = dto.Activo;
        recurso.HorarioAperturaDefault = dto.HorarioAperturaDefault;
        recurso.HorarioCierreDefault = dto.HorarioCierreDefault;

        await _context.SaveChangesAsync();

        return new RecursoReservableDto
        {
            Id = recurso.Id,
            Nombre = recurso.Nombre,
            TipoDeporte = recurso.TipoDeporte,
            Activo = recurso.Activo,
            HorarioAperturaDefault = recurso.HorarioAperturaDefault,
            HorarioCierreDefault = recurso.HorarioCierreDefault
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recurso = await _context.RecursosReservables
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recurso is null)
            return false;

        _context.RecursosReservables.Remove(recurso);
        await _context.SaveChangesAsync();

        return true;
    }
}
