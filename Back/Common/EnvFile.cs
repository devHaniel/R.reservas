// Copyright (c) 2026 Haniel Hernández. All rights reserved under the MIT license.

namespace Reservas.Common;

/// <summary>
/// Carga un archivo .env local sin sobrescribir variables ya definidas por el
/// sistema. En Docker o producción las variables del entorno tienen prioridad.
/// </summary>
public static class EnvFile
{
    public static void Load(string fileName = ".env")
    {
        var path = FindUp(fileName);
        if (path is null)
            return;

        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line[7..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            if (key.Length == 0)
                continue;

            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            // Si la clave se repite, gana la última aparición del archivo.
            values[key] = value;
        }

        foreach (var (key, value) in values)
        {
            // Respeta las variables que ya existan en el entorno.
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindUp(string fileName)
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }
        }

        return null;
    }
}
