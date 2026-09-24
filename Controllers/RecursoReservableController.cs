using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.RecursoReservable;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[ApiController]
[Route("api/recursos-reservables")]
public class RecursoReservableController : ControllerBase
{
    private readonly IRecursoReservableService _service;

    public RecursoReservableController(IRecursoReservableService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecursoReservableDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecursoReservableDto>> GetById(int id)
    {
        var recurso = await _service.GetByIdAsync(id);
        return recurso is null ? NotFound() : Ok(recurso);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<RecursoReservableDto>>> GetByTipoDeporte([FromQuery] string tipoDeporte)
    {
        return Ok(await _service.GetByTipoDeporteAsync(tipoDeporte));
    }

    [HttpPost]
    public async Task<ActionResult<RecursoReservableDto>> Create(RecursoReservableCrearDto dto)
    {
        var recurso = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = recurso.Id }, recurso);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RecursoReservableDto>> Update(int id, RecursoReservableActualizarDto dto)
    {
        if (id != dto.Id)
            return BadRequest("El id de la ruta no coincide con el id del recurso.");

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