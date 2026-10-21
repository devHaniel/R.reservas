using Microsoft.EntityFrameworkCore;
using Reservas.Common.Email;
using Reservas.Common.Fechas;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Options;
using Reservas.Services.Interfaces;

namespace Reservas.Services.Email;

/// <summary>
/// Envía un recordatorio por correo para las reservas que empiezan dentro de
/// la ventana configurada. La marca <see cref="Reserva.RecordatorioEnviadoEn"/>
/// evita duplicados; si el envío falla, la reserva queda sin marcar y se
/// reintenta en la siguiente pasada.
/// </summary>
public sealed class RecordatorioReservasWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PublicBookingOptions _options;
    private readonly ILogger<RecordatorioReservasWorker> _logger;

    public RecordatorioReservasWorker(
        IServiceScopeFactory scopeFactory,
        PublicBookingOptions options,
        ILogger<RecordatorioReservasWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Deja terminar el arranque y las migraciones antes de consultar la base.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al procesar los recordatorios de reservas");
            }

            var intervalo = TimeSpan.FromMinutes(
                Math.Max(1, _options.RecordatorioIntervaloMinutos));

            try
            {
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcesarAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var ahora = DateTime.Now;
        var limite = ahora.AddHours(Math.Max(1, _options.RecordatorioHorasAntes));

        var reservas = await db.Reservas
            .Include(r => r.Cliente)
            .Include(r => r.RecursoReservable)
            .Where(r =>
                r.RecordatorioEnviadoEn == null &&
                r.Estado != EstadoReserva.Cancelada &&
                r.Estado != EstadoReserva.Completada &&
                r.Cliente.Email != null &&
                r.Cliente.Email != "" &&
                r.FechaHoraInicio > ahora &&
                r.FechaHoraInicio <= limite)
            .OrderBy(r => r.FechaHoraInicio)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (reservas.Count == 0)
            return;

        var enviados = 0;

        foreach (var reserva in reservas)
        {
            var destino = reserva.Cliente.Email;
            if (string.IsNullOrWhiteSpace(destino))
                continue;

            try
            {
                await email.EnviarRecordatorioReservaAsync(
                    destino.Trim(),
                    new ReservaEmailInfo(
                        $"R-{reserva.Id:D6}",
                        reserva.Cliente.Nombre,
                        reserva.RecursoReservable.Nombre,
                        reserva.NombreServicio,
                        FechaHoraApi.Formatear(reserva.FechaHoraInicio),
                        FechaHoraApi.Formatear(reserva.FechaHoraFin),
                        reserva.Precio,
                        reserva.Estado),
                    cancellationToken);

                reserva.RecordatorioEnviadoEn = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                enviados++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "No se pudo enviar el recordatorio de la reserva {ReservaId} a {Email}",
                    reserva.Id,
                    destino);
            }
        }

        if (enviados > 0)
        {
            _logger.LogInformation(
                "Se enviaron {Cantidad} recordatorios de reservas",
                enviados);
        }
    }
}
