using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reservas.Common.DTOs.Public;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

/// <summary>
/// API anónima usada por la landing pública. No expone datos internos de
/// clientes ni reservas: solo catálogo, horarios libres y el resultado de la
/// propia reserva creada.
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
[EnableRateLimiting("public-policy")]
public class PublicController : ControllerBase
{
    private readonly IPublicBookingService _service;

    public PublicController(IPublicBookingService service)
    {
        _service = service;
    }

    [HttpGet("catalogo")]
    public async Task<ActionResult<List<PublicEspacioDto>>> GetCatalogo(
        CancellationToken cancellationToken)
        => Ok(await _service.GetCatalogoAsync(cancellationToken));

    [HttpGet("opciones/{tipoServicioId:int}/disponibilidad")]
    public async Task<ActionResult<PublicDisponibilidadDto>> GetDisponibilidad(
        int tipoServicioId,
        [FromQuery] string fecha,
        CancellationToken cancellationToken)
        => Ok(await _service.GetDisponibilidadAsync(tipoServicioId, fecha, cancellationToken));

    [HttpPost("verificacion/solicitar")]
    public async Task<ActionResult<PublicVerificacionSolicitadaDto>> SolicitarVerificacion(
        PublicVerificacionSolicitudDto dto,
        CancellationToken cancellationToken)
        => Ok(await _service.SolicitarVerificacionAsync(dto, cancellationToken));

    [HttpPost("verificacion/confirmar")]
    public async Task<ActionResult<PublicVerificacionConfirmadaDto>> ConfirmarVerificacion(
        PublicVerificacionConfirmacionDto dto,
        CancellationToken cancellationToken)
        => Ok(await _service.ConfirmarVerificacionAsync(dto, cancellationToken));

    [HttpPost("reservas")]
    public async Task<ActionResult<PublicReservaDto>> CrearReserva(
        PublicReservaCrearDto dto,
        CancellationToken cancellationToken)
    {
        var reserva = await _service.CrearReservaAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, reserva);
    }
}
