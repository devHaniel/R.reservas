using System.Globalization;

namespace Reservas.Common.Fechas;

/// <summary>
/// Convención única de fecha/hora para la API.
///
/// La aplicación trabaja con la hora local del negocio (la misma que se configura en
/// <c>HorarioAperturaDefault</c> / <c>HorarioCierreDefault</c>). Por eso el transporte
/// hacia/desde el frontend es un string "wall-clock" sin zona horaria:
///
///     yyyy-MM-ddTHH:mm      ->  "2026-10-10T16:00"
///
/// - Entrada: se aceptan también "yyyy-MM-dd HH:mm", con segundos y con sufijo "Z"
///   (la "Z" se ignora porque la hora ya representa la hora local del negocio;
///   un offset explícito como "+02:00" se rechaza para evitar ambigüedad).
/// - Salida: SIEMPRE en el formato canónico, independiente de la cultura del servidor,
///   para que el frontend lo muestre manipulándolo como string (sin re-interpretar zona).
/// </summary>
public static class FechaHoraApi
{
    /// <summary>Formato canónico de transporte (sin segundos, sin zona).</summary>
    public const string Formato = "yyyy-MM-ddTHH:mm";

    /// <summary>Ejemplo del formato esperado, útil para mensajes de error.</summary>
    public const string Ejemplo = "2026-10-10T16:00";

    private static readonly string[] FormatosEntrada =
    [
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd"
    ];

    /// <summary>
    /// Parsea una fecha enviada por el cliente. Lanza <see cref="ArgumentException"/>
    /// (que el middleware traduce a 400) si el valor es vacío o inválido.
    /// </summary>
    public static DateTime Parse(string? valor, string nombreCampo = "fecha")
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException(
                $"El campo '{nombreCampo}' es obligatorio. Formato esperado: {Ejemplo}.");

        // Se ignora deliberadamente cualquier zona horaria ('Z' o '+hh:mm'):
        // el valor representa la hora local del negocio, no un instante UTC.
        var limpio = valor.Trim();
        if (limpio.EndsWith('Z') || limpio.EndsWith('z'))
            limpio = limpio[..^1].Trim();

        if (!DateTime.TryParseExact(
                limpio,
                FormatosEntrada,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fecha))
        {
            throw new ArgumentException(
                $"El campo '{nombreCampo}' tiene un formato inválido ('{valor}'). " +
                $"Use el formato {Formato}. Ejemplo: {Ejemplo}.");
        }

        // Kind = Unspecified: se trata como hora local del negocio, sin conversiones.
        return DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified);
    }

    /// <summary>Formatea una fecha para enviarla al frontend en el formato canónico.</summary>
    public static string Formatear(DateTime fecha)
        => fecha.ToString(Formato, CultureInfo.InvariantCulture);

    /// <summary>
    /// Formatea un instante UTC agregando el sufijo 'Z', para campos de auditoría
    /// (por ejemplo fecha de creación), que sí representan un instante absoluto.
    /// </summary>
    public static string FormatearUtc(DateTime fechaUtc)
        => Formatear(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Unspecified)) + "Z";
}