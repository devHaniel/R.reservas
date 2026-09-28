using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.TipoServicio;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[ApiController]
[Route("api/tipos-servicio")]
[Authorize(Roles = "Admin,Vendedor")]
public class TipoServicioController : ControllerBase
{
    private readonly ITipoServicioService _service;

    public TipoServicioController(ITipoServicioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<TipoServicioDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoServicioDto>> GetById(int id)
    {
        var tipo = await _service.GetByIdAsync(id);
        return tipo is null ? NotFound() : Ok(tipo);
    }

    [HttpGet("por-recurso/{recursoReservableId:int}")]
    public async Task<ActionResult<List<TipoServicioDto>>> GetByRecurso(int recursoReservableId)
        => Ok(await _service.GetByRecursoAsync(recursoReservableId));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TipoServicioDto>> Create(TipoServicioCrearDto dto)
    {
        var tipo = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = tipo.Id }, tipo);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TipoServicioDto>> Update(int id, TipoServicioActualizarDto dto)
    {
        if (id != dto.Id)
            return BadRequest("El id de la ruta no coincide con el id del tipo de servicio.");

        return Ok(await _service.UpdateAsync(dto));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}