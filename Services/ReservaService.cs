using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.Reserva;
using Reservas.Data;
using Reservas.Hubs;
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
            .AsNoTracking()
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => new ReservaDto
        {
            Id = r.Id,
            ClienteId = r.ClienteId,
            RecursoReservableId = r.RecursoReservableId,
            TipoServicioId = r.TipoServicioId,
            FechaHoraInicio = r.FechaHoraInicio,
            FechaHoraFin = r.FechaHoraFin,
            Estado = r.Estado,
            FechaCreacion = r.FechaCreacion
        }).ToList();
    }

    public async Task<ReservaDto?> GetByIdAsync(int id)
    {
        var reserva = await _context.Reservas
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
            return null;

        return new ReservaDto
        {
            Id = reserva.Id,
            ClienteId = reserva.ClienteId,
            RecursoReservableId = reserva.RecursoReservableId,
            TipoServicioId = reserva.TipoServicioId,
            FechaHoraInicio = reserva.FechaHoraInicio,
            FechaHoraFin = reserva.FechaHoraFin,
            Estado = reserva.Estado,
            FechaCreacion = reserva.FechaCreacion
        };
    }

    public async Task<List<ReservaDto>> GetByClienteAsync(int clienteId)
    {
        var reservas = await _context.Reservas
            .AsNoTracking()
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => new ReservaDto
        {
            Id = r.Id,
            ClienteId = r.ClienteId,
            RecursoReservableId = r.RecursoReservableId,
            TipoServicioId = r.TipoServicioId,
            FechaHoraInicio = r.FechaHoraInicio,
            FechaHoraFin = r.FechaHoraFin,
            Estado = r.Estado,
            FechaCreacion = r.FechaCreacion
        }).ToList();
    }

    public async Task<List<ReservaDto>> GetByRecursoAsync(int recursoReservableId)
    {
        var reservas = await _context.Reservas
            .AsNoTracking()
            .Where(r => r.RecursoReservableId == recursoReservableId)
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => new ReservaDto
        {
            Id = r.Id,
            ClienteId = r.ClienteId,
            RecursoReservableId = r.RecursoReservableId,
            TipoServicioId = r.TipoServicioId,
            FechaHoraInicio = r.FechaHoraInicio,
            FechaHoraFin = r.FechaHoraFin,
            Estado = r.Estado,
            FechaCreacion = r.FechaCreacion
        }).ToList();
    }

    public async Task<ReservaDto> CreateAsync(ReservaCrearDto dto)
    {
        var recursoReservable = await _context.RecursosReservables
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == dto.RecursoReservableId);

        if (recursoReservable is null)
            throw new KeyNotFoundException("Recurso reservable no encontrado");

        var tipoServicio = await _context.TiposServicio
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dto.TipoServicioId);

        if (tipoServicio is null)
            throw new KeyNotFoundException("Tipo de servicio no encontrado");

        if (dto.FechaHoraInicio.CompareTo(recursoReservable.HorarioCierreDefault) > 0 || dto.FechaHoraFin.CompareTo(recursoReservable.HorarioAperturaDefault) < 0)
            throw new ArgumentException("La reserva está fuera del horario permitido para este recurso.");
        
        if (dto.FechaHoraFin <= dto.FechaHoraInicio)
            throw new ArgumentException("La fecha y hora de fin debe ser posterior a la fecha y hora de inicio.");
        var reserva = new Reserva
        {
            ClienteId = dto.ClienteId,
            RecursoReservableId = dto.RecursoReservableId,
            TipoServicioId = dto.TipoServicioId,
            FechaHoraInicio = dto.FechaHoraInicio,
            FechaHoraFin = dto.FechaHoraFin,
            Estado = dto.Estado,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return new ReservaDto
        {
            Id = reserva.Id,
            ClienteId = reserva.ClienteId,
            RecursoReservableId = reserva.RecursoReservableId,
            TipoServicioId = reserva.TipoServicioId,
            FechaHoraInicio = reserva.FechaHoraInicio,
            FechaHoraFin = reserva.FechaHoraFin,
            Estado = reserva.Estado,
            FechaCreacion = reserva.FechaCreacion
        };
    }

    public async Task<ReservaDto> UpdateAsync(ReservaActualizarDto dto)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == dto.Id);

        if (reserva is null)
            throw new KeyNotFoundException("Reserva no encontrada");

        var recursoReservable = await _context.RecursosReservables
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == dto.RecursoReservableId);

        if (recursoReservable is null)
            throw new KeyNotFoundException("Recurso reservable no encontrado");

        var tipoServicio = await _context.TiposServicio
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dto.TipoServicioId);

        if (tipoServicio is null)
            throw new KeyNotFoundException("Tipo de servicio no encontrado");

        
        reserva.ClienteId = dto.ClienteId;
        reserva.RecursoReservableId = dto.RecursoReservableId;
        reserva.TipoServicioId = dto.TipoServicioId;
        reserva.FechaHoraInicio = dto.FechaHoraInicio;
        reserva.FechaHoraFin = dto.FechaHoraFin;
        reserva.Estado = dto.Estado;

        await _context.SaveChangesAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");


        return new ReservaDto
        {
            Id = reserva.Id,
            ClienteId = reserva.ClienteId,
            RecursoReservableId = reserva.RecursoReservableId,
            TipoServicioId = reserva.TipoServicioId,
            FechaHoraInicio = reserva.FechaHoraInicio,
            FechaHoraFin = reserva.FechaHoraFin,
            Estado = reserva.Estado,
            FechaCreacion = reserva.FechaCreacion
        };
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

}
