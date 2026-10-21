using Reservas.Common.DTOs.Public;

namespace Reservas.Services.Interfaces;

public interface IPublicBookingService
{
    Task<List<PublicEspacioDto>> GetCatalogoAsync(CancellationToken cancellationToken = default);

    Task<PublicDisponibilidadDto> GetDisponibilidadAsync(
        int tipoServicioId,
        string fecha,
        CancellationToken cancellationToken = default);

    Task<PublicVerificacionSolicitadaDto> SolicitarVerificacionAsync(
        PublicVerificacionSolicitudDto dto,
        CancellationToken cancellationToken = default);

    Task<PublicVerificacionConfirmadaDto> ConfirmarVerificacionAsync(
        PublicVerificacionConfirmacionDto dto,
        CancellationToken cancellationToken = default);

    Task<PublicReservaDto> CrearReservaAsync(
        PublicReservaCrearDto dto,
        CancellationToken cancellationToken = default);
}
