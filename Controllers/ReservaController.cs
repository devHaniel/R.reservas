using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Reservas.Common.DTOs.Reserva;
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
    {
        return Ok(await _service.GetAllAsync(UserId, EsAdmin));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservaDto>> GetById(int id)
    {
        var reserva = await _service.GetByIdAsync(id, UserId, EsAdmin);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpGet("por-cliente/{clienteId:int}")]
    public async Task<ActionResult<List<ReservaDto>>> GetByCliente(int clienteId)
    {
        return Ok(await _service.GetByClienteAsync(clienteId, UserId, EsAdmin));
    }

    [HttpGet("por-recurso/{recursoReservableId:int}")]
    public async Task<ActionResult<List<ReservaDto>>> GetByRecurso(int recursoReservableId)
    {
        return Ok(await _service.GetByRecursoAsync(recursoReservableId, UserId, EsAdmin));
    }

    [HttpPost]
    public async Task<ActionResult<ReservaDto>> Create(ReservaCrearDto dto)
    {
        try
        {
            var reserva = await _service.CreateAsync(dto, UserId, EsAdmin);
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
            return Ok(await _service.UpdateAsync(dto, UserId, EsAdmin));
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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _service.DeleteAsync(id, UserId, EsAdmin) ? NoContent() : NotFound();
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool EsAdmin => User.IsInRole("Admin");
}