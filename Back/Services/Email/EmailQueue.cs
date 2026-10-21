using System.Threading.Channels;
using Reservas.Common.Email;
using Reservas.Services.Interfaces;

namespace Reservas.Services.Email;

public sealed record EmailJob(string Email, ReservaEmailInfo Reserva, bool EsRecordatorio);

public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailJob> _channel = Channel.CreateUnbounded<EmailJob>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    public ChannelReader<EmailJob> Reader => _channel.Reader;

    public void EncolarConfirmacion(string email, ReservaEmailInfo reserva) =>
        _channel.Writer.TryWrite(new EmailJob(email, reserva, false));

    public void EncolarRecordatorio(string email, ReservaEmailInfo reserva) =>
        _channel.Writer.TryWrite(new EmailJob(email, reserva, true));
}
