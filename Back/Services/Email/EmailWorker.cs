using Reservas.Services.Interfaces;

namespace Reservas.Services.Email;

/// <summary>
/// Procesa la cola de correos de reservas fuera del ciclo de la petición.
/// Un fallo de SMTP no rompe la reserva: queda registrado para diagnóstico.
/// </summary>
public sealed class EmailWorker : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailWorker> _logger;

    public EmailWorker(
        EmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                if (job.EsRecordatorio)
                    await email.EnviarRecordatorioReservaAsync(job.Email, job.Reserva, stoppingToken);
                else
                    await email.EnviarConfirmacionReservaAsync(job.Email, job.Reserva, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "No se pudo enviar el correo de reserva {Folio} a {Email}",
                    job.Reserva.Folio,
                    job.Email);
            }
        }
    }
}
