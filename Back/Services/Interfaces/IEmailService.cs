using Reservas.Common.Email;

namespace Reservas.Services.Interfaces;

public interface IEmailService
{
    bool EstaConfigurado { get; }

    Task EnviarCodigoVerificacionAsync(
        string email,
        string codigo,
        CancellationToken cancellationToken = default);

    Task EnviarConfirmacionReservaAsync(
        string email,
        ReservaEmailInfo reserva,
        CancellationToken cancellationToken = default);

    Task EnviarRecordatorioReservaAsync(
        string email,
        ReservaEmailInfo reserva,
        CancellationToken cancellationToken = default);
}
