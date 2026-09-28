using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reservas.Common.DTOs.Reserva;
using Reservas.Common.Fechas;
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
        var reservas = await BaseQuery()
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<ReservaDto?> GetByIdAsync(int id)
    {
        var reserva = await BaseQuery()
            .FirstOrDefaultAsync(r => r.Id == id);

        return reserva is null ? null : Map(reserva);
    }

    public async Task<List<ReservaDto>> GetByClienteAsync(int clienteId)
    {
        var reservas = await BaseQuery()
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<List<ReservaDto>> GetByRecursoAsync(int recursoReservableId)
    {
        var reservas = await BaseQuery()
            .Where(r => r.RecursoReservableId == recursoReservableId)
            .OrderByDescending(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<List<ReservaDto>> GetByRangoAsync(
        string? desde,
        string? hasta,
        int? recursoReservableId)
    {
        DateTime? inicio = string.IsNullOrWhiteSpace(desde)
            ? null
            : FechaHoraApi.Parse(desde, nameof(desde));
        DateTime? fin = string.IsNullOrWhiteSpace(hasta)
            ? null
            : FechaHoraApi.Parse(hasta, nameof(hasta));

        if (inicio.HasValue && fin.HasValue && inicio > fin)
            throw new ArgumentException(
                $"'{nameof(desde)}' no puede ser posterior a '{nameof(hasta)}'.");

        var query = BaseQuery();

        if (recursoReservableId.HasValue)
            query = query.Where(r => r.RecursoReservableId == recursoReservableId.Value);

        // Solapamiento: la reserva no termina antes del inicio NI empieza después del fin.
        if (fin.HasValue)
            query = query.Where(r => r.FechaHoraInicio <= fin.Value);

        if (inicio.HasValue)
            query = query.Where(r => r.FechaHoraFin >= inicio.Value);

        var reservas = await query
            .OrderBy(r => r.FechaHoraInicio)
            .ToListAsync();

        return reservas.Select(r => Map(r)).ToList();
    }

    public async Task<ReservaDto> CreateAsync(ReservaCrearDto dto)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.EliminadoEn == null)
            ?? throw new KeyNotFoundException("Cliente no encontrado");

        var tipoServicio = await CargarServicioValidoAsync(dto.TipoServicioId);

        var recurso = tipoServicio.RecursoReservable;
        var inicio = FechaHoraApi.Parse(dto.FechaHoraInicio, nameof(dto.FechaHoraInicio));
        var fin = inicio.AddMinutes(tipoServicio.DuracionMinutos);

        ValidarHorario(recurso, inicio, fin);
        await AsegurarSinConflictoAsync(recurso.Id, inicio, fin);

        var reserva = new Reserva
        {
            ClienteId = cliente.Id,
            TipoServicioId = tipoServicio.Id,
            RecursoReservableId = recurso.Id,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Estado = dto.Estado,
            FechaCreacion = DateTime.UtcNow,
            Precio = tipoServicio.Precio,
            DuracionMinutos = tipoServicio.DuracionMinutos,
            NombreServicio = tipoServicio.Nombre,
            Nota = dto.Nota
        };

        _context.Reservas.Add(reserva);
        await GuardarReservaAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return Map(reserva, cliente.Nombre, recurso.Nombre);
    }

    public async Task<ReservaDto> UpdateAsync(ReservaActualizarDto dto)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == dto.Id)
            ?? throw new KeyNotFoundException("Reserva no encontrada");

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.EliminadoEn == null)
            ?? throw new KeyNotFoundException("Cliente no encontrado");

        var tipoServicio = await CargarServicioValidoAsync(dto.TipoServicioId);

        var recurso = tipoServicio.RecursoReservable;
        var inicio = FechaHoraApi.Parse(dto.FechaHoraInicio, nameof(dto.FechaHoraInicio));
        var fin = inicio.AddMinutes(tipoServicio.DuracionMinutos);

        ValidarHorario(recurso, inicio, fin);
        await AsegurarSinConflictoAsync(recurso.Id, inicio, fin, reserva.Id);

        reserva.ClienteId = cliente.Id;
        reserva.TipoServicioId = tipoServicio.Id;
        reserva.RecursoReservableId = recurso.Id;
        reserva.FechaHoraInicio = inicio;
        reserva.FechaHoraFin = fin;
        reserva.Estado = dto.Estado;
        reserva.Nota = dto.Nota;

        // Nuevo snapshot: el servicio puede haber cambiado de precio/duración.
        reserva.Precio = tipoServicio.Precio;
        reserva.DuracionMinutos = tipoServicio.DuracionMinutos;
        reserva.NombreServicio = tipoServicio.Nombre;

        await GuardarReservaAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return Map(reserva, cliente.Nombre, recurso.Nombre);
    }

    public async Task<ReservaDto?> CambiarEstadoAsync(int id, EstadoReserva estado)
    {
        var reserva = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        if (reserva is null)
            return null;

        // Si se reactiva una reserva cancelada, hay que revalidar el turno.
        if (reserva.Estado == EstadoReserva.Cancelada && estado != EstadoReserva.Cancelada)
            await AsegurarSinConflictoAsync(
                reserva.RecursoReservableId,
                reserva.FechaHoraInicio,
                reserva.FechaHoraFin,
                reserva.Id);

        reserva.Estado = estado;
        await GuardarReservaAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return Map(reserva);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Solo Admin llega aquí (ver controlador). Es una salida de emergencia
        // para entradas erróneas; el flujo normal es cancelar (cambiar estado).
        var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.Id == id);
        if (reserva is null)
            return false;

        _context.Reservas.Remove(reserva);
        await _context.SaveChangesAsync();

        await _notificaciones.NotificarAsync("ReservaActualizado");

        return true;
    }

    // --- Helpers ---

    private IQueryable<Reserva> BaseQuery()
        => _context.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.RecursoReservable)
            .AsNoTracking();

    private async Task<TipoServicio> CargarServicioValidoAsync(int tipoServicioId)
    {
        var tipoServicio = await _context.TiposServicio
            .Include(t => t.RecursoReservable)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tipoServicioId)
            ?? throw new KeyNotFoundException("Tipo de servicio no encontrado");

        if (tipoServicio.EliminadoEn != null || !tipoServicio.Activo)
            throw new ArgumentException("El tipo de servicio no está disponible.");

        if (tipoServicio.RecursoReservable.EliminadoEn != null ||
            !tipoServicio.RecursoReservable.Activo)
            throw new ArgumentException("El recurso no está disponible.");

        return tipoServicio;
    }

    private static void ValidarHorario(RecursoReservable recurso, DateTime inicio, DateTime fin)
    {
        if (inicio.TimeOfDay < recurso.HorarioAperturaDefault ||
            fin.TimeOfDay > recurso.HorarioCierreDefault)
            throw new ArgumentException("La reserva está fuera del horario permitido para este recurso.");
    }

    private async Task AsegurarSinConflictoAsync(
        int recursoReservableId,
        DateTime inicio,
        DateTime fin,
        int? excluirReservaId = null)
    {
        var hayConflicto = await _context.Reservas
            .Where(r =>
                r.RecursoReservableId == recursoReservableId &&
                r.Estado != EstadoReserva.Cancelada &&
                (!excluirReservaId.HasValue || r.Id != excluirReservaId.Value))
            .AnyAsync(r => r.FechaHoraInicio < fin && r.FechaHoraFin > inicio);

        if (hayConflicto)
            throw new ArgumentException("El recurso ya está reservado en el horario indicado.");
    }

    private async Task GuardarReservaAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            // La exclusion constraint rechazó un solapamiento que se coló entre la
            // verificación previa y el insert (petición concurrente).
            throw new ArgumentException("El recurso ya está reservado en el horario indicado.");
        }
    }

    private static ReservaDto Map(
        Reserva reserva,
        string? nombreCliente = null,
        string? nombreRecurso = null)
    {
        return new ReservaDto
        {
            Id = reserva.Id,
            ClienteId = reserva.ClienteId,
            NombreCliente = nombreCliente ?? reserva.Cliente?.Nombre ?? string.Empty,
            RecursoReservableId = reserva.RecursoReservableId,
            NombreRecurso = nombreRecurso ?? reserva.RecursoReservable?.Nombre ?? string.Empty,
            TipoServicioId = reserva.TipoServicioId,
            NombreServicio = reserva.NombreServicio,
            FechaHoraInicio = FechaHoraApi.Formatear(reserva.FechaHoraInicio),
            FechaHoraFin = FechaHoraApi.Formatear(reserva.FechaHoraFin),
            Estado = reserva.Estado,
            Precio = reserva.Precio,
            DuracionMinutos = reserva.DuracionMinutos,
            Nota = reserva.Nota,
            FechaCreacion = FechaHoraApi.FormatearUtc(reserva.FechaCreacion)
        };
    }
}