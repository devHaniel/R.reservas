using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.Reserva;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[ApiController]
[Route("api/reservas")]
public class ReservaController : ControllerBase
{
    private readonly IReservaService _service;

    public ReservaController(IReservaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReservaDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservaDto>> GetById(int id)
    {
        var reserva = await _service.GetByIdAsync(id);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpGet("por-cliente/{clienteId:int}")]
    public async Task<ActionResult<List<ReservaDto>>> GetByCliente(int clienteId)
    {
        return Ok(await _service.GetByClienteAsync(clienteId));
    }

    [HttpGet("por-recurso/{recursoReservableId:int}")]
    public async Task<ActionResult<List<ReservaDto>>> GetByRecurso(int recursoReservableId)
    {
        return Ok(await _service.GetByRecursoAsync(recursoReservableId));
    }

    [HttpPost]
    public async Task<ActionResult<ReservaDto>> Create(ReservaCrearDto dto)
    {
        try
        {
            var reserva = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = reserva.Id }, reserva);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReservaDto>> Update(int id, ReservaActualizarDto dto)
    {
        if (id != dto.Id)
            return BadRequest("El id de la ruta no coincide con el id de la reserva.");

        try
        {
            return Ok(await _service.UpdateAsync(dto));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}