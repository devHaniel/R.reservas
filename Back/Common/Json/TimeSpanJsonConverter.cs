using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reservas.Common.Json;

/// <summary>
/// Convierte <see cref="TimeSpan"/> hacia/desde horas "de reloj" en formato corto.
///
/// - Salida: "HH:mm" (ej. "08:00", "23:30").
/// - Entrada: acepta "HH:mm", "HH:mm:ss" y el formato extendido con días.
///
/// Así un <c>&lt;input type="time"&gt;</c> de Vue, que produce "08:00", se envía tal cual.
/// </summary>
public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    private static readonly string[] FormatosEntrada =
    [
        "c",
        @"d\.hh\:mm\:ss",
        @"hh\:mm\:ss",
        @"hh\:mm",
        @"h\:mm"
    ];

    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Se esperaba un string con una hora (ej. \"08:00\").");

        var valor = reader.GetString()?.Trim();
        if (string.IsNullOrEmpty(valor))
            return default;

        if (TimeSpan.TryParseExact(
                valor,
                FormatosEntrada,
                CultureInfo.InvariantCulture,
                out var hora))
        {
            return hora;
        }

        throw new JsonException(
            $"Hora inválida '{valor}'. Use el formato HH:mm. Ejemplo: \"08:00\".");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteStringValue(
            value.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
}