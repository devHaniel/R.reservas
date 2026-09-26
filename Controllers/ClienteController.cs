using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.Cliente;
using Reservas.Common.Paginacion;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[Authorize]
[ApiController]
[Route("api/clientes")]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _service;

    public ClienteController(IClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClienteDto>>> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidad = 10)
    {
        if (pagina < 1 || cantidad < 1 || cantidad > 100)
            return BadRequest("La página debe ser mayor que 0 y la cantidad debe estar entre 1 y 100.");

        return Ok(await _service.GetAllAsync(pagina, cantidad));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        var cliente = await _service.GetByIdAsync(id);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<ClienteDto>>> GetByEmail([FromQuery] string email)
    {
        return Ok(await _service.GetByEmailAsync(email));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(ClienteCrearDto dto)
    {
        var cliente = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Update(int id, ClienteActualizarDto dto)
    {
        if (id != dto.Id)
            return BadRequest("El id de la ruta no coincide con el id del cliente.");

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