namespace Reservas.Options;

/// <summary>
/// Configuración del correo saliente. Por defecto usa Gmail: solo hacen falta
/// el correo (<see cref="User"/>) y la contraseña de aplicación. Si no hay
/// credenciales, en Desarrollo se muestra el código en la respuesta y en el log;
/// en Producción se rechaza el envío.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public string FromName { get; set; } = "Reservas";
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Si no se define un remitente distinto, se usa el mismo correo de Gmail.
    /// </summary>
    public string Remitente =>
        string.IsNullOrWhiteSpace(From) ? User ?? string.Empty : From;

    /// <summary>
    /// Gmail queda configurado en cuanto existan correo y contraseña. También se
    /// admite un SMTP sin autenticación si se define Host y From sin User.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Remitente) &&
        (string.IsNullOrWhiteSpace(User) || !string.IsNullOrWhiteSpace(Password));
}
