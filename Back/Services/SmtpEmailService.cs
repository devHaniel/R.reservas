using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Reservas.Common.Email;
using Reservas.Common.Fechas;
using Reservas.Models.Entities;
using Reservas.Options;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

/// <summary>
/// Envío de correo con SMTP. En Desarrollo, si no hay SMTP configurado, el
/// código de verificación se escribe en el log y la API lo devuelve para poder
/// probar el flujo sin credenciales externas. En Producción nunca se expone.
/// Los correos de reserva se omiten con un aviso en Desarrollo y fallan en
/// Producción para que el worker los reintente.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private static readonly CultureInfo CulturaCorreo = new("es-MX");

    private readonly SmtpOptions _smtp;
    private readonly PublicBookingOptions _booking;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        SmtpOptions smtp,
        PublicBookingOptions booking,
        IHostEnvironment environment,
        ILogger<SmtpEmailService> logger)
    {
        _smtp = smtp;
        _booking = booking;
        _environment = environment;
        _logger = logger;
    }

    public bool EstaConfigurado => _smtp.IsConfigured;

    public async Task EnviarCodigoVerificacionAsync(
        string email,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        if (!PrepararEnvio(
                email,
                $"Código de verificación para {email}: {codigo}"))
        {
            return;
        }

        var negocio = WebUtility.HtmlEncode(_booking.NombreNegocio);
        var asunto = $"Tu código de verificación · {_booking.NombreNegocio}";

        var html = $"""
            <!doctype html>
            <html lang="es">
              <body style="margin:0;padding:0;background:#f4f5ef;color:#1f2f28;font-family:Arial,Helvetica,sans-serif;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f5ef;padding:32px 16px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:520px;background:#ffffff;border:1px solid #e4e7df;border-radius:12px;overflow:hidden;">
                        <tr>
                          <td style="padding:28px 30px 12px;">
                            <div style="font-size:20px;font-weight:800;color:#1b5a45;">{negocio}<span style="color:#e0694f;">.</span></div>
                            <p style="margin:18px 0 6px;font-size:22px;font-weight:700;color:#1f2f28;">Verifica tu correo</p>
                            <p style="margin:0;font-size:14px;line-height:1.6;color:#6d7770;">
                              Usa este código para confirmar que eres una persona real y completar tu reserva.
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td align="center" style="padding:18px 30px 10px;">
                            <div style="display:inline-block;padding:16px 26px;border-radius:10px;background:#eef5ed;border:1px solid #d8e7d5;color:#173f31;font-size:34px;font-weight:800;letter-spacing:9px;">{codigo}</div>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:8px 30px 28px;text-align:center;">
                            <p style="margin:0;font-size:13px;line-height:1.6;color:#7a847d;">
                              El código vence en {_booking.CodigoVigenciaMinutos} minutos.<br />
                              Si no solicitaste esta reserva, puedes ignorar este mensaje.
                            </p>
                          </td>
                        </tr>
                      </table>
                      <p style="max-width:520px;margin:16px auto 0;font-size:11px;line-height:1.5;color:#98a09a;text-align:center;">
                        Este es un mensaje automático de {negocio}. No respondas a este correo.
                      </p>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        await EnviarAsync(email, asunto, html, cancellationToken);
    }

    public Task EnviarConfirmacionReservaAsync(
        string email,
        ReservaEmailInfo reserva,
        CancellationToken cancellationToken = default)
    {
        var confirmada = reserva.Estado == EstadoReserva.Confirmada;
        var titulo = confirmada ? "Tu reserva está confirmada" : "Recibimos tu solicitud";
        var intro = confirmada
            ? "Tu reserva quedó confirmada. Guarda estos datos y llega unos minutos antes."
            : "Recibimos tu solicitud. El equipo revisará la disponibilidad y te confirmará por este mismo medio.";
        var asunto = confirmada
            ? $"Reserva confirmada · {_booking.NombreNegocio}"
            : $"Solicitud de reserva recibida · {_booking.NombreNegocio}";

        return EnviarReservaAsync(
            email,
            reserva,
            asunto,
            titulo,
            intro,
            cancellationToken);
    }

    public Task EnviarRecordatorioReservaAsync(
        string email,
        ReservaEmailInfo reserva,
        CancellationToken cancellationToken = default)
    {
        var asunto = $"Recordatorio de tu reserva · {_booking.NombreNegocio}";
        var titulo = "Te esperamos pronto";
        var intro = "Te recordamos tu próxima reserva. Si necesitas cambiarla, contacta al negocio.";

        return EnviarReservaAsync(
            email,
            reserva,
            asunto,
            titulo,
            intro,
            cancellationToken);
    }

    private async Task EnviarReservaAsync(
        string email,
        ReservaEmailInfo reserva,
        string asunto,
        string titulo,
        string intro,
        CancellationToken cancellationToken)
    {
        if (!PrepararEnvio(email, $"Correo '{asunto}' para {email} (SMTP no configurado)."))
            return;

        var negocio = WebUtility.HtmlEncode(_booking.NombreNegocio);
        var nombreCliente = WebUtility.HtmlEncode(reserva.NombreCliente);
        var folio = WebUtility.HtmlEncode(reserva.Folio);
        var espacio = WebUtility.HtmlEncode(reserva.NombreEspacio);
        var servicio = WebUtility.HtmlEncode(reserva.NombreServicio);
        var fecha = WebUtility.HtmlEncode(FormatearFechaHora(reserva.FechaHoraInicio, reserva.FechaHoraFin));
        var precio = WebUtility.HtmlEncode(FormatearPrecio(reserva.Precio));
        var estado = WebUtility.HtmlEncode(TextoEstado(reserva.Estado));
        var estadoColor = reserva.Estado == EstadoReserva.Confirmada ? "#1b5a45" : "#8a5a12";
        var estadoFondo = reserva.Estado == EstadoReserva.Confirmada ? "#eef5ed" : "#fff6e5";
        var tituloHtml = WebUtility.HtmlEncode(titulo);
        var introHtml = WebUtility.HtmlEncode(intro);

        var html = $"""
            <!doctype html>
            <html lang="es">
              <body style="margin:0;padding:0;background:#f4f5ef;color:#1f2f28;font-family:Arial,Helvetica,sans-serif;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f5ef;padding:32px 16px;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border:1px solid #e4e7df;border-radius:12px;overflow:hidden;">
                        <tr>
                          <td style="padding:28px 30px 10px;">
                            <div style="font-size:20px;font-weight:800;color:#1b5a45;">{negocio}<span style="color:#e0694f;">.</span></div>
                            <p style="margin:18px 0 6px;font-size:22px;font-weight:700;color:#1f2f28;">{tituloHtml}</p>
                            <p style="margin:0;font-size:14px;line-height:1.6;color:#6d7770;">
                              Hola {nombreCliente}. {introHtml}
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:18px 30px 6px;">
                            <div style="display:inline-block;padding:6px 10px;border-radius:999px;background:{estadoFondo};color:{estadoColor};font-size:12px;font-weight:700;">
                              {estado}
                            </div>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:12px 30px 8px;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;font-size:14px;line-height:1.5;">
                              <tr>
                                <td style="padding:9px 0;color:#7a847d;border-bottom:1px solid #eef0ea;">Folio</td>
                                <td style="padding:9px 0;text-align:right;font-weight:700;border-bottom:1px solid #eef0ea;">{folio}</td>
                              </tr>
                              <tr>
                                <td style="padding:9px 0;color:#7a847d;border-bottom:1px solid #eef0ea;">Espacio</td>
                                <td style="padding:9px 0;text-align:right;font-weight:700;border-bottom:1px solid #eef0ea;">{espacio}</td>
                              </tr>
                              <tr>
                                <td style="padding:9px 0;color:#7a847d;border-bottom:1px solid #eef0ea;">Servicio</td>
                                <td style="padding:9px 0;text-align:right;font-weight:700;border-bottom:1px solid #eef0ea;">{servicio}</td>
                              </tr>
                              <tr>
                                <td style="padding:9px 0;color:#7a847d;border-bottom:1px solid #eef0ea;">Fecha y hora</td>
                                <td style="padding:9px 0;text-align:right;font-weight:700;border-bottom:1px solid #eef0ea;">{fecha}</td>
                              </tr>
                              <tr>
                                <td style="padding:9px 0;color:#7a847d;">Total</td>
                                <td style="padding:9px 0;text-align:right;font-weight:700;">{precio}</td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:12px 30px 28px;">
                            <p style="margin:0;font-size:13px;line-height:1.6;color:#7a847d;">
                              Si no reconoces esta reserva, responde a este correo o contacta al negocio.
                            </p>
                          </td>
                        </tr>
                      </table>
                      <p style="max-width:560px;margin:16px auto 0;font-size:11px;line-height:1.5;color:#98a09a;text-align:center;">
                        Este es un mensaje automático de {negocio}. No respondas a este correo.
                      </p>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        await EnviarAsync(email, asunto, html, cancellationToken);
    }

    private bool PrepararEnvio(string email, string detalleDesarrollo)
    {
        if (_smtp.IsConfigured)
            return true;

        if (_environment.IsDevelopment())
        {
            _logger.LogWarning(
                "SMTP no configurado. {Detalle}",
                detalleDesarrollo);
            return false;
        }

        throw new InvalidOperationException(
            "El envío de correos no está configurado. Contacta al administrador.");
    }

    private async Task EnviarAsync(
        string email,
        string asunto,
        string html,
        CancellationToken cancellationToken)
    {
        using var cliente = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20_000
        };

        if (!string.IsNullOrWhiteSpace(_smtp.User))
        {
            cliente.Credentials = new NetworkCredential(
                _smtp.User,
                _smtp.Password ?? string.Empty);
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_smtp.Remitente, _smtp.FromName, Encoding.UTF8),
            Subject = asunto,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = html
        };

        mensaje.To.Add(new MailAddress(email));
        await cliente.SendMailAsync(mensaje, cancellationToken);
    }

    private static string FormatearFechaHora(string inicioTexto, string finTexto)
    {
        try
        {
            var inicio = FechaHoraApi.Parse(inicioTexto, "inicio");
            var fin = FechaHoraApi.Parse(finTexto, "fin");
            var fecha = inicio.ToString("dddd d 'de' MMMM 'de' yyyy", CulturaCorreo);
            fecha = char.ToUpper(fecha[0], CulturaCorreo) + fecha[1..];

            return $"{fecha} · {inicio:HH:mm} – {fin:HH:mm}";
        }
        catch (ArgumentException)
        {
            return $"{inicioTexto} – {finTexto}";
        }
    }

    private static string FormatearPrecio(decimal precio)
        => precio.ToString("C", CulturaCorreo);

    private static string TextoEstado(EstadoReserva estado)
        => estado switch
        {
            EstadoReserva.Pendiente => "Pendiente de confirmación",
            EstadoReserva.Confirmada => "Confirmada",
            EstadoReserva.Cancelada => "Cancelada",
            EstadoReserva.Completada => "Completada",
            _ => estado.ToString()
        };
}
