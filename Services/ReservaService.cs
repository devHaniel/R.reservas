using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.Reserva;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class ReservaService : IReservaService
{
    private readonly AppDbContext _context;
    private readonly IRealtimeNotificacionService _notificaciones;

    public ReservaService(AppDbContext context, IRealtimeNotificacionService notificaciones)
    {
        _context = context;
        _notificaciones = notificaciones;
    }

    public async Task<List<ReservaDto>> GetAllAsync()
    {
        var reservas = await _context.Reservas
            .Include(r => r.TipoServicio)
            .AsNoTracking()
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<ReservaDto?> GetByIdAsync(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.TipoServicio)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return null;

        return Map(reserva);
    }

    public async Task<List<ReservaDto>> GetByClienteAsync(int clienteId)
    {
        var reservas = await _context.Reservas
            .Include(r => r.TipoServicio)
            .AsNoTracking()
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<List<ReservaDto>> GetByRecursoAsync(int recursoReservableId)
    {
        var reservas = await _context.Reservas
            .Include(r => r.TipoServicio)
            .AsNoTracking()
            .Where(r => r.TipoServicio.RecursoReservableId == recursoReservableId)
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<ReservaDto> CreateAsync(ReservaCrearDto dto)
    {
        var tipoServicio = await _context.TiposServicio
            .Include(t => t.RecursoReservable)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dto.TipoServicioId);

        if (tipoServicio is null)
            throw new KeyNotFoundException("Tipo de servicio no encontrado");

        var recursoReservable = tipoServicio.RecursoReservable;
        var fechaHoraFin = dto.FechaHoraInicio.AddMinutes(tipoServicio.DuracionMinutos);

        if (dto.FechaHoraInicio.TimeOfDay < recursoReservable.HorarioAperturaDefault ||
            fechaHoraFin.TimeOfDay > recursoReservable.HorarioCierreDefault)
            throw new ArgumentException("La reserva está fuera del horario permitido para este recurso.");

        if (await ExisteConflictoAsync(
                recursoReservable.Id,
                dto.FechaHoraInicio,
                fechaHoraFin))
        {
            throw new ArgumentException(
                "El recurso ya está reservado en el horario indicado.");
        }

        var reserva = new Reserva
        {
            ClienteId = dto.ClienteId,
            TipoServicioId = dto.TipoServicioId,
            FechaHoraInicio = dto.FechaHoraInicio,
            FechaHoraFin = fechaHoraFin,
            Estado = dto.Estado,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return Map(reserva, recursoReservable.Id);
    }

    public async Task<ReservaDto> UpdateAsync(ReservaActualizarDto dto)
    {
        var reserva = await _context.Reservas
            .Include(r => r.TipoServicio)
            .FirstOrDefaultAsync(r => r.Id == dto.Id);

        if (reserva is null)
            throw new KeyNotFoundException("Reserva no encontrada");

        var tipoServicio = await _context.TiposServicio
            .Include(t => t.RecursoReservable)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dto.TipoServicioId);

        if (tipoServicio is null)
            throw new KeyNotFoundException("Tipo de servicio no encontrado");

        var recursoReservable = tipoServicio.RecursoReservable;
        var fechaHoraFin = dto.FechaHoraInicio.AddMinutes(tipoServicio.DuracionMinutos);

        if (dto.FechaHoraInicio.TimeOfDay < recursoReservable.HorarioAperturaDefault ||
            fechaHoraFin.TimeOfDay > recursoReservable.HorarioCierreDefault)
            throw new ArgumentException("La reserva está fuera del horario permitido para este recurso.");

        if (await ExisteConflictoAsync(
                recursoReservable.Id,
                dto.FechaHoraInicio,
                fechaHoraFin,
                reserva.Id))
        {
            throw new ArgumentException(
                "El recurso ya está reservado en el horario indicado.");
        }

        reserva.ClienteId = dto.ClienteId;
        reserva.TipoServicioId = dto.TipoServicioId;
        reserva.FechaHoraInicio = dto.FechaHoraInicio;
        reserva.FechaHoraFin = fechaHoraFin;
        reserva.Estado = dto.Estado;

        await _context.SaveChangesAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");


        return Map(reserva, recursoReservable.Id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return false;

        _context.Reservas.Remove(reserva);
        await _context.SaveChangesAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return true;
    }

    private async Task<bool> ExisteConflictoAsync(
        int recursoReservableId,
        DateTime fechaHoraInicio,
        DateTime fechaHoraFin,
        int? reservaIdExcluir = null)
    {
        return await _context.Reservas
            .Where(r =>
                r.TipoServicio.RecursoReservableId == recursoReservableId &&
                r.Estado != EstadoReserva.Cancelada &&
                (!reservaIdExcluir.HasValue || r.Id != reservaIdExcluir.Value))
            .AnyAsync(r =>
                r.FechaHoraInicio < fechaHoraFin &&
                r.FechaHoraFin > fechaHoraInicio);
    }

    private static ReservaDto Map(Reserva reserva, int? recursoReservableId = null)
    {
        return new ReservaDto
        {
            Id = reserva.Id,
            ClienteId = reserva.ClienteId,
            RecursoReservableId = recursoReservableId ?? reserva.TipoServicio.RecursoReservableId,
            TipoServicioId = reserva.TipoServicioId,
            FechaHoraInicio = reserva.FechaHoraInicio,
            FechaHoraFin = reserva.FechaHoraFin,
            Estado = reserva.Estado,
            FechaCreacion = reserva.FechaCreacion
        };
    }

}
