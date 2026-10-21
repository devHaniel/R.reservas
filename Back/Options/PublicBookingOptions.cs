namespace Reservas.Options;

/// <summary>
/// Reglas de la reserva pública. El "pepper" se usa para firmar con HMAC los
/// códigos y tokens antes de guardarlos; si no se configura, Program.cs usa la
/// clave JWT para evitar que quede un valor predecible por defecto.
/// </summary>
public class PublicBookingOptions
{
    public const string SectionName = "PublicBooking";

    public string NombreNegocio { get; set; } = "Reservas";
    public int CodigoVigenciaMinutos { get; set; } = 10;
    public int TokenVigenciaMinutos { get; set; } = 15;
    public int MaxIntentosCodigo { get; set; } = 5;
    public int ReenviarSegundos { get; set; } = 60;
    public int DiasMaximosAnticipacion { get; set; } = 90;
    public string Pepper { get; set; } = string.Empty;

    /// <summary>Horas de antelación con las que se envía el recordatorio de una reserva.</summary>
    public int RecordatorioHorasAntes { get; set; } = 24;

    /// <summary>Cada cuántos minutos el worker busca reservas próximas para recordar.</summary>
    public int RecordatorioIntervaloMinutos { get; set; } = 15;
}
