using Reservas.Common.Email;

namespace Reservas.Services.Interfaces;

/// <summary>
/// Cola en memoria para no bloquear la creación de una reserva mientras el
/// servidor SMTP responde. El worker se encarga de enviar y registrar errores.
/// </summary>
public interface IEmailQueue
{
    void EncolarConfirmacion(string email, ReservaEmailInfo reserva);

    void EncolarRecordatorio(string email, ReservaEmailInfo reserva);
}
