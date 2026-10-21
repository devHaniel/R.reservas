using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Reservas.Common.DTOs.Auth;
using Reservas.Models.Entities;

namespace Reservas.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private static readonly string[] RolesValidos = ["Admin", "Vendedor"];

    private readonly UserManager<Usuario> _userManager;

    public UsuariosController(UserManager<Usuario> userManager)
    {
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
    public async Task<ActionResult<AuthUserDto>> Create(CrearUsuarioDto dto)
    {
        var rol = string.IsNullOrWhiteSpace(dto.Rol) ? "Vendedor" : dto.Rol.Trim();
        if (!RolesValidos.Contains(rol))
            return BadRequest("El rol debe ser Admin o Vendedor.");

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

        var roleResult = await _userManager.AddToRoleAsync(usuario, rol);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(usuario);
            return BadRequest(new ValidationProblemDetails(roleResult.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description })));
        }

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

        if (!string.IsNullOrWhiteSpace(dto.Rol) && !RolesValidos.Contains(dto.Rol))
            return BadRequest("El rol debe ser Admin o Vendedor.");

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();

        var rolesActuales = await _userManager.GetRolesAsync(usuario);
        var eraAdmin = rolesActuales.Contains("Admin");
        var seraAdmin = string.IsNullOrWhiteSpace(dto.Rol) ? eraAdmin : dto.Rol == "Admin";

        if (id == CurrentUserId && eraAdmin && !seraAdmin)
            return BadRequest("No puedes quitarte el rol Admin a ti mismo.");

        if (eraAdmin && !seraAdmin && await EsUnicoAdminAsync(usuario))
            return BadRequest("Debe existir al menos un administrador activo.");

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Email = dto.Email.Trim();
        usuario.UserName = dto.Email.Trim();

        var result = await _userManager.UpdateAsync(usuario);
        if (!result.Succeeded)
            return BadRequest(new ValidationProblemDetails(result.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description })));

        if (!string.IsNullOrWhiteSpace(dto.Rol))
        {
            foreach (var role in rolesActuales.Except([dto.Rol]))
                await _userManager.RemoveFromRoleAsync(usuario, role);

            if (!rolesActuales.Contains(dto.Rol))
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
        if (id == CurrentUserId)
            return BadRequest("No puedes eliminar tu propia cuenta.");

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound();

        if (await EsUnicoAdminAsync(usuario))
            return BadRequest("Debe existir al menos un administrador activo.");

        var result = await _userManager.DeleteAsync(usuario);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return NoContent();
    }

    private async Task<bool> EsUnicoAdminAsync(Usuario usuario)
    {
        if (!await _userManager.IsInRoleAsync(usuario, "Admin"))
            return false;

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        return admins.Count <= 1;
    }
}