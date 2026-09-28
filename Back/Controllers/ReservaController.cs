using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.Reserva;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize(Roles = "Admin,Vendedor")]
public class ReservaController : ControllerBase
{
    private readonly IReservaService _service;

    public ReservaController(IReservaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReservaDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservaDto>> GetById(int id)
    {
        var reserva = await _service.GetByIdAsync(id);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpGet("por-cliente/{clienteId:int}")]
    public async Task<ActionResult<List<ReservaDto>>> GetByCliente(int clienteId)
        => Ok(await _service.GetByClienteAsync(clienteId));

    [HttpGet("por-recurso/{recursoReservableId:int}")]
    public async Task<ActionResult<List<ReservaDto>>> GetByRecurso(int recursoReservableId)
        => Ok(await _service.GetByRecursoAsync(recursoReservableId));

    /// <summary>
    /// Reservas que se solapan con un rango de fechas (hora local, "yyyy-MM-ddTHH:mm").
    /// Ideal para vistas de calendario. Ambos extremos son opcionales.
    /// </summary>
    [HttpGet("por-fechas")]
    public async Task<ActionResult<List<ReservaDto>>> GetByFechas(
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromQuery] int? recursoReservableId)
        => Ok(await _service.GetByRangoAsync(desde, hasta, recursoReservableId));

    [HttpPost]
    public async Task<ActionResult<ReservaDto>> Create(ReservaCrearDto dto)
    {
        var reserva = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = reserva.Id }, reserva);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReservaDto>> Update(int id, ReservaActualizarDto dto)
    {
        if (id != dto.Id)
            return BadRequest("El id de la ruta no coincide con el id de la reserva.");

        return Ok(await _service.UpdateAsync(dto));
    }

    /// <summary>Cambia solo el estado (ej. cancelar o confirmar) sin tocar el resto.</summary>
    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<ReservaDto>> CambiarEstado(int id, CambiarEstadoReservaDto dto)
    {
        var reserva = await _service.CambiarEstadoAsync(id, dto.Estado);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}