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
        var recursos = await Activos().ToListAsync();
        return recursos.Select(Map).ToList();
    }

    public async Task<RecursoReservableDto?> GetByIdAsync(int id)
    {
        var recurso = await Activos().FirstOrDefaultAsync(r => r.Id == id);
        return recurso is null ? null : Map(recurso);
    }

    public async Task<List<RecursoReservableDto>> GetByTipoDeporteAsync(string tipoDeporte)
    {
        var filtro = tipoDeporte.Trim().ToUpper();
        var recursos = await Activos()
            .Where(r => r.TipoDeporte.ToUpper().Contains(filtro))
            .ToListAsync();

        return recursos.Select(Map).ToList();
    }

    public async Task<RecursoReservableDto> CreateAsync(RecursoReservableCrearDto dto)
    {
        ValidarHorario(dto.HorarioAperturaDefault, dto.HorarioCierreDefault);

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

        return Map(recurso);
    }

    public async Task<RecursoReservableDto> UpdateAsync(RecursoReservableActualizarDto dto)
    {
        ValidarHorario(dto.HorarioAperturaDefault, dto.HorarioCierreDefault);

        var recurso = await _context.RecursosReservables
            .FirstOrDefaultAsync(r => r.Id == dto.Id && r.EliminadoEn == null)
            ?? throw new KeyNotFoundException("Recurso reservable no encontrado");

        recurso.Nombre = dto.Nombre;
        recurso.TipoDeporte = dto.TipoDeporte;
        recurso.Activo = dto.Activo;
        recurso.HorarioAperturaDefault = dto.HorarioAperturaDefault;
        recurso.HorarioCierreDefault = dto.HorarioCierreDefault;

        await _context.SaveChangesAsync();

        return Map(recurso);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recurso = await _context.RecursosReservables
            .FirstOrDefaultAsync(r => r.Id == id && r.EliminadoEn == null);

        if (recurso is null)
            return false;

        // Borrado lógico: conservamos reservas históricas y servicios asociados.
        recurso.EliminadoEn = DateTime.UtcNow;
        recurso.Activo = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private IQueryable<RecursoReservable> Activos()
        => _context.RecursosReservables
            .AsNoTracking()
            .Where(r => r.EliminadoEn == null)
            .OrderBy(r => r.Nombre);

    private static void ValidarHorario(TimeSpan apertura, TimeSpan cierre)
    {
        if (apertura >= cierre)
            throw new ArgumentException("El horario de apertura debe ser anterior al de cierre.");
    }

    private static RecursoReservableDto Map(RecursoReservable r) => new()
    {
        Id = r.Id,
        Nombre = r.Nombre,
        TipoDeporte = r.TipoDeporte,
        Activo = r.Activo,
        HorarioAperturaDefault = r.HorarioAperturaDefault,
        HorarioCierreDefault = r.HorarioCierreDefault
    };
}