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

    public async Task<List<RecursoReservableDto>> GetByCategoriaAsync(string categoria)
    {
        var filtro = categoria.Trim().ToUpper();
        var recursos = await Activos()
            .Where(r => r.Categoria.ToUpper().Contains(filtro))
            .ToListAsync();

        return recursos.Select(Map).ToList();
    }

    public async Task<RecursoReservableDto> CreateAsync(RecursoReservableCrearDto dto)
    {
        ValidarHorario(dto.HorarioAperturaDefault, dto.HorarioCierreDefault);

        var recurso = new RecursoReservable
        {
            Nombre = dto.Nombre,
            Categoria = dto.Categoria,
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
        recurso.Categoria = dto.Categoria;
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
        // Las opciones del espacio también se archivan para que dejen de ofrecerse.
        var ahora = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync();

        recurso.EliminadoEn = ahora;
        recurso.Activo = false;
        await _context.SaveChangesAsync();

        await _context.TiposServicio
            .Where(t => t.RecursoReservableId == id && t.EliminadoEn == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.EliminadoEn, ahora)
                .SetProperty(t => t.Activo, false));

        await transaction.CommitAsync();

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
        Categoria = r.Categoria,
        Activo = r.Activo,
        HorarioAperturaDefault = r.HorarioAperturaDefault,
        HorarioCierreDefault = r.HorarioCierreDefault
    };
}