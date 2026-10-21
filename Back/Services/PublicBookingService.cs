using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.Public;
using Reservas.Common.DTOs.Reserva;
using Reservas.Common.Fechas;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Options;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

/// <summary>
/// Flujo público de reservas: catálogo, disponibilidad, verificación de correo
/// y creación de la reserva. No hay cuentas de cliente: el correo verificado es
/// la prueba de identidad mínima y el cliente se crea o actualiza en el
/// directorio interno al confirmar.
/// </summary>
public class PublicBookingService : IPublicBookingService
{
    private static readonly string[] FormatosFecha = ["yyyy-MM-dd"];

    private readonly AppDbContext _context;
    private readonly IReservaService _reservas;
    private readonly IEmailService _email;
    private readonly PublicBookingOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly byte[] _pepper;

    public PublicBookingService(
        AppDbContext context,
        IReservaService reservas,
        IEmailService email,
        PublicBookingOptions options,
        IHostEnvironment environment)
    {
        _context = context;
        _reservas = reservas;
        _email = email;
        _options = options;
        _environment = environment;
        _pepper = Encoding.UTF8.GetBytes(options.Pepper);
    }

    public async Task<List<PublicEspacioDto>> GetCatalogoAsync(
        CancellationToken cancellationToken = default)
    {
        var recursos = await _context.RecursosReservables
            .AsNoTracking()
            .Include(r => r.ServiciosDisponibles.Where(s =>
                s.Activo && s.EliminadoEn == null))
            .Where(r => r.Activo && r.EliminadoEn == null)
            .OrderBy(r => r.Categoria)
            .ThenBy(r => r.Nombre)
            .ToListAsync(cancellationToken);

        return recursos
            .Select(r =>
            {
                var opciones = r.ServiciosDisponibles
                    .OrderBy(s => s.Precio)
                    .ThenBy(s => s.Nombre)
                    .Select(s => new PublicOpcionDto
                    {
                        Id = s.Id,
                        Nombre = s.Nombre,
                        DuracionMinutos = s.DuracionMinutos,
                        Precio = s.Precio
                    })
                    .ToList();

                return new PublicEspacioDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Categoria = r.Categoria,
                    HorarioApertura = FormatearHora(r.HorarioAperturaDefault),
                    HorarioCierre = FormatearHora(r.HorarioCierreDefault),
                    PrecioDesde = opciones.Count > 0 ? opciones.Min(o => o.Precio) : 0,
                    Opciones = opciones
                };
            })
            .Where(r => r.Opciones.Count > 0)
            .ToList();
    }

    public async Task<PublicDisponibilidadDto> GetDisponibilidadAsync(
        int tipoServicioId,
        string fecha,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParseExact(
                fecha?.Trim(),
                FormatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dia))
        {
            throw new ArgumentException("La fecha debe tener el formato yyyy-MM-dd.");
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (dia < hoy)
            throw new ArgumentException("La fecha ya pasó. Elige un día a partir de hoy.");
        if (dia > hoy.AddDays(_options.DiasMaximosAnticipacion))
            throw new ArgumentException(
                $"Solo puedes reservar con {_options.DiasMaximosAnticipacion} días de anticipación.");

        var opcion = await CargarOpcionPublicaAsync(tipoServicioId, cancellationToken);
        var recurso = opcion.RecursoReservable;
        var inicioDia = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var finDia = inicioDia.AddDays(1);

        var reservas = await _context.Reservas
            .AsNoTracking()
            .Where(r =>
                r.RecursoReservableId == recurso.Id &&
                r.Estado != EstadoReserva.Cancelada &&
                r.FechaHoraInicio < finDia &&
                r.FechaHoraFin > inicioDia)
            .Select(r => new { r.FechaHoraInicio, r.FechaHoraFin })
            .ToListAsync(cancellationToken);

        var apertura = inicioDia.Add(recurso.HorarioAperturaDefault);
        var cierre = inicioDia.Add(recurso.HorarioCierreDefault);
        var ahora = DateTime.Now;
        var horarios = new List<PublicHorarioDto>();

        for (var inicio = apertura;
             inicio.AddMinutes(opcion.DuracionMinutos) <= cierre;
             inicio = inicio.AddMinutes(30))
        {
            var fin = inicio.AddMinutes(opcion.DuracionMinutos);
            if (inicio <= ahora)
                continue;
            if (reservas.Any(r => inicio < r.FechaHoraFin && fin > r.FechaHoraInicio))
                continue;

            horarios.Add(new PublicHorarioDto
            {
                HoraInicio = inicio.ToString("HH:mm", CultureInfo.InvariantCulture),
                HoraFin = fin.ToString("HH:mm", CultureInfo.InvariantCulture)
            });
        }

        return new PublicDisponibilidadDto
        {
            OpcionId = opcion.Id,
            NombreOpcion = opcion.Nombre,
            Fecha = dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DuracionMinutos = opcion.DuracionMinutos,
            Horarios = horarios
        };
    }

    public async Task<PublicVerificacionSolicitadaDto> SolicitarVerificacionAsync(
        PublicVerificacionSolicitudDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizarEmail(dto.Email);
        var ahora = DateTime.UtcNow;

        var ultima = await _context.VerificacionesCorreo
            .AsNoTracking()
            .Where(v => v.Email == email)
            .OrderByDescending(v => v.CreadoEn)
            .FirstOrDefaultAsync(cancellationToken);

        if (ultima is not null)
        {
            var espera = ultima.CreadoEn.AddSeconds(_options.ReenviarSegundos) - ahora;
            if (espera > TimeSpan.Zero)
            {
                var segundos = Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds));
                throw new ArgumentException(
                    $"Espera {segundos} segundos antes de solicitar otro código.");
            }
        }

        // Un código nuevo invalida los anteriores que sigan vigentes.
        await _context.VerificacionesCorreo
            .Where(v => v.Email == email && v.CodigoUsadoEn == null && v.CodigoExpiraEn > ahora)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(v => v.CodigoUsadoEn, ahora),
                cancellationToken);

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var verificacion = new VerificacionCorreo
        {
            Email = email,
            CodigoHash = Hash($"{email}:{codigo}"),
            CodigoExpiraEn = ahora.AddMinutes(_options.CodigoVigenciaMinutos),
            CreadoEn = ahora
        };

        _context.VerificacionesCorreo.Add(verificacion);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _email.EnviarCodigoVerificacionAsync(email, codigo, cancellationToken);
        }
        catch
        {
            _context.VerificacionesCorreo.Remove(verificacion);
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }

        return new PublicVerificacionSolicitadaDto
        {
            Mensaje = "Revisa tu correo e ingresa el código de 6 dígitos. También puede estar en spam.",
            ExpiraEnSegundos = _options.CodigoVigenciaMinutos * 60,
            ReenviarEnSegundos = _options.ReenviarSegundos,
            CodigoDesarrollo = !_email.EstaConfigurado && _environment.IsDevelopment()
                ? codigo
                : null
        };
    }

    public async Task<PublicVerificacionConfirmadaDto> ConfirmarVerificacionAsync(
        PublicVerificacionConfirmacionDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizarEmail(dto.Email);
        var codigo = dto.Codigo.Trim();
        var ahora = DateTime.UtcNow;

        var verificacion = await _context.VerificacionesCorreo
            .Where(v =>
                v.Email == email &&
                v.CodigoUsadoEn == null &&
                v.CodigoExpiraEn > ahora)
            .OrderByDescending(v => v.CreadoEn)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ArgumentException(
                "El código expiró o no existe. Solicita uno nuevo.");

        if (verificacion.Intentos >= _options.MaxIntentosCodigo)
            throw new ArgumentException(
                "Demasiados intentos con este código. Solicita uno nuevo.");

        var esperado = Hash($"{email}:{codigo}");
        if (!HashEquals(verificacion.CodigoHash, esperado))
        {
            verificacion.Intentos++;
            await _context.SaveChangesAsync(cancellationToken);

            var restantes = Math.Max(0, _options.MaxIntentosCodigo - verificacion.Intentos);
            throw new ArgumentException(
                restantes == 0
                    ? "Código incorrecto. Solicita un código nuevo."
                    : $"Código incorrecto. Te quedan {restantes} intentos.");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        verificacion.CodigoUsadoEn = ahora;
        verificacion.TokenHash = Hash($"{email}:{token}");
        verificacion.TokenExpiraEn = ahora.AddMinutes(_options.TokenVigenciaMinutos);
        await _context.SaveChangesAsync(cancellationToken);

        return new PublicVerificacionConfirmadaDto
        {
            Token = token,
            ExpiraEnSegundos = _options.TokenVigenciaMinutos * 60,
            Mensaje = "Correo verificado. Ya puedes confirmar tu reserva."
        };
    }

    public async Task<PublicReservaDto> CrearReservaAsync(
        PublicReservaCrearDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizarEmail(dto.Email);
        var nombre = dto.Nombre.Trim();
        var telefono = dto.Telefono.Trim();
        var token = dto.TokenVerificacion.Trim();

        if (nombre.Length == 0 || telefono.Length == 0 || token.Length == 0)
            throw new ArgumentException("Completa tus datos y verifica el correo antes de reservar.");

        var ahoraUtc = DateTime.UtcNow;
        var tokenHash = Hash($"{email}:{token}");
        var verificacion = await _context.VerificacionesCorreo
            .FirstOrDefaultAsync(
                v =>
                    v.Email == email &&
                    v.TokenHash == tokenHash &&
                    v.TokenUsadoEn == null &&
                    v.TokenExpiraEn > ahoraUtc,
                cancellationToken)
            ?? throw new ArgumentException(
                "La verificación del correo expiró. Solicita un código nuevo y vuelve a intentarlo.");

        var opcion = await CargarOpcionPublicaAsync(dto.TipoServicioId, cancellationToken);
        var recurso = opcion.RecursoReservable;
        var inicio = FechaHoraApi.Parse(dto.FechaHoraInicio, nameof(dto.FechaHoraInicio));
        var fin = inicio.AddMinutes(opcion.DuracionMinutos);

        if (inicio < DateTime.Now)
            throw new ArgumentException("La fecha y hora ya pasaron. Elige otro horario.");
        if (inicio.Date > DateTime.Today.AddDays(_options.DiasMaximosAnticipacion))
            throw new ArgumentException(
                $"Solo puedes reservar con {_options.DiasMaximosAnticipacion} días de anticipación.");
        if (inicio.TimeOfDay < recurso.HorarioAperturaDefault ||
            fin.TimeOfDay > recurso.HorarioCierreDefault)
            throw new ArgumentException("El horario está fuera de la atención de este espacio.");

        var ocupado = await _context.Reservas
            .AnyAsync(
                r =>
                    r.RecursoReservableId == recurso.Id &&
                    r.Estado != EstadoReserva.Cancelada &&
                    r.FechaHoraInicio < fin &&
                    r.FechaHoraFin > inicio,
                cancellationToken);

        if (ocupado)
            throw new ArgumentException(
                "Ese horario acaba de ocuparse. Elige otro horario disponible.");

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(
                c =>
                    c.EliminadoEn == null &&
                    c.Email != null &&
                    c.Email.ToLower() == email,
                cancellationToken);

        // Si el personal ya registró a la persona solo con teléfono, la
        // verificamos por correo y completamos su ficha en vez de chocar con el
        // índice único del teléfono.
        cliente ??= await _context.Clientes
            .FirstOrDefaultAsync(
                c =>
                    c.EliminadoEn == null &&
                    c.Email == null &&
                    c.Telefono == telefono,
                cancellationToken);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                UsuarioId = null,
                Nombre = nombre,
                Telefono = telefono,
                Email = email
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (!string.Equals(cliente.Telefono, telefono, StringComparison.Ordinal))
            {
                var telefonoOcupado = await _context.Clientes
                    .AnyAsync(
                        c =>
                            c.EliminadoEn == null &&
                            c.Id != cliente.Id &&
                            c.Telefono == telefono,
                        cancellationToken);

                if (telefonoOcupado)
                    throw new ArgumentException(
                        "Ese teléfono ya está asociado a otro cliente. Usa el número con el que te registraste o contacta al negocio.");

                cliente.Telefono = telefono;
            }

            cliente.Nombre = nombre;
            cliente.Email = email;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var reserva = await _reservas.CreateAsync(new ReservaCrearDto
        {
            ClienteId = cliente.Id,
            TipoServicioId = opcion.Id,
            FechaHoraInicio = dto.FechaHoraInicio,
            Estado = EstadoReserva.Pendiente,
            Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim()
        });

        // El token es de un solo uso: se consume después de que la reserva se
        // guardó correctamente.
        verificacion.TokenUsadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new PublicReservaDto
        {
            Id = reserva.Id,
            Folio = $"R-{reserva.Id:D6}",
            NombreCliente = reserva.NombreCliente,
            NombreRecurso = reserva.NombreRecurso,
            NombreServicio = reserva.NombreServicio,
            FechaHoraInicio = reserva.FechaHoraInicio,
            FechaHoraFin = reserva.FechaHoraFin,
            Estado = reserva.Estado,
            Precio = reserva.Precio,
            DuracionMinutos = reserva.DuracionMinutos,
            Mensaje = "Recibimos tu reserva. El equipo la confirmará y te contactará si hace falta."
        };
    }

    private async Task<TipoServicio> CargarOpcionPublicaAsync(
        int tipoServicioId,
        CancellationToken cancellationToken)
    {
        return await _context.TiposServicio
            .AsNoTracking()
            .Include(t => t.RecursoReservable)
            .FirstOrDefaultAsync(
                t =>
                    t.Id == tipoServicioId &&
                    t.Activo &&
                    t.EliminadoEn == null &&
                    t.RecursoReservable.Activo &&
                    t.RecursoReservable.EliminadoEn == null,
                cancellationToken)
            ?? throw new KeyNotFoundException("La opción de reserva no está disponible.");
    }

    private string Hash(string value)
    {
        using var hmac = new HMACSHA256(_pepper);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static bool HashEquals(string izquierda, string derecha)
    {
        var a = Encoding.UTF8.GetBytes(izquierda);
        var b = Encoding.UTF8.GetBytes(derecha);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string NormalizarEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string FormatearHora(TimeSpan hora) =>
        hora.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
}
