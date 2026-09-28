using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Reservas.Common.DTOs.Auth;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IAuthService _authService;

    public UsuariosController(UserManager<Usuario> userManager, IAuthService authService)
    {
        _userManager = userManager;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthUserDto>>> GetAll()
    {
        var usuarios = _userManager.Users.ToList();
        var authUsers = new List<AuthUserDto>();
        foreach (var u in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(u);
            authUsers.Add(new AuthUserDto
            {
                id = u.Id.ToString(),
                email = u.Email!,
                name = u.Nombre,
                roles = roles.ToArray()
            });
        }
        return Ok(authUsers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthUserDto>> GetById(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(usuario);

        return Ok(new AuthUserDto
        {
            id = usuario.Id.ToString(),
            email = usuario.Email!,
            name = usuario.Nombre,
            roles = roles.ToArray()
        });
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<AuthUserDto>> GetByEmail([FromQuery] string email)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(usuario);

        return Ok(new AuthUserDto
        {
            id = usuario.Id.ToString(),
            email = usuario.Email!,
            name = usuario.Nombre,
            roles = roles.ToArray()
        });
    }

    [HttpPost]
    public async Task<ActionResult<AuthUserDto>> Create(RegistroUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = dto.Email.Trim(),
            UserName = dto.Email.Trim()
        };

        var result = await _userManager.CreateAsync(usuario, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new ValidationProblemDetails(result.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description })));
        }

        await _userManager.AddToRoleAsync(usuario, "Vendedor");

        var roles = await _userManager.GetRolesAsync(usuario);

        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, new AuthUserDto
        {
            id = usuario.Id.ToString(),
            email = usuario.Email!,
            name = usuario.Nombre,
            roles = roles.ToArray()
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AuthUserDto>> Update(int id, ActualizarUsuarioDto dto)
    {
        if (id != dto.Id)
            return BadRequest("El id de la ruta no coincide con el id del usuario.");

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();

        usuario.Nombre = dto.Nombre;
        usuario.Email = dto.Email;

        var result = await _userManager.UpdateAsync(usuario);
        if (!result.Succeeded)
            return BadRequest(new ValidationProblemDetails(result.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description })));

        // If role is being updated, remove from old role and add new
        if (!string.IsNullOrEmpty(dto.Rol))
        {
            await _userManager.RemoveFromRoleAsync(usuario, "Vendedor");
            await _userManager.AddToRoleAsync(usuario, dto.Rol);
        }

        var roles = await _userManager.GetRolesAsync(usuario);

        return Ok(new AuthUserDto
        {
            id = usuario.Id.ToString(),
            email = usuario.Email!,
            name = usuario.Nombre,
            roles = roles.ToArray()
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();

        var result = await _userManager.DeleteAsync(usuario);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return NoContent();
    }
}