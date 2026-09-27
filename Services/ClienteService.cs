using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.Cliente;
using Reservas.Common.Paginacion;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _context;

    public ClienteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ClienteDto>> GetAllAsync(
        int usuarioId,
        bool esAdmin,
        int pagina = 1,
        int cantidad = 10)
    {
        var query = _context.Clientes
            .AsNoTracking()
            .Where(cliente => esAdmin || cliente.UsuarioId == usuarioId);

        var total = await query.CountAsync();

        var clientes = await query
        .OrderBy(c => c.Id)
        .Skip((pagina - 1) * cantidad)
        .Take(cantidad)
        .ToListAsync();

        var clientesdto = clientes.Select(c => new ClienteDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Telefono = c.Telefono,
            Email = c.Email
        }).ToList();

        return new PagedResult<ClienteDto>
        {
            Items = clientesdto,
            Pagina = pagina,
            Cantidad = cantidad,
            Total = total
        };
    }

    public async Task<ClienteDto?> GetByIdAsync(int id, int usuarioId, bool esAdmin)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && (esAdmin || c.UsuarioId == usuarioId));

        if (cliente is null)
            return null;

        return new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Telefono = cliente.Telefono,
            Email = cliente.Email
        };
    }

    public async Task<ClienteDto?> GetByEmailAsync(string email, int usuarioId, bool esAdmin)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email != null
                && c.Email.ToUpper() == email.ToUpper()
                && (esAdmin || c.UsuarioId == usuarioId));

        if (cliente is null)
            return null;

        return new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Telefono = cliente.Telefono,
            Email = cliente.Email
        };
    }

    public async Task<ClienteDto> CreateAsync(ClienteCrearDto dto, int usuarioId, bool esAdmin)
    {
        var cliente = new Cliente
        {
            UsuarioId = esAdmin ? null : usuarioId,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Email = dto.Email
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Telefono = cliente.Telefono,
            Email = cliente.Email
        };
    }

    public async Task<ClienteDto> UpdateAsync(ClienteActualizarDto dto, int usuarioId, bool esAdmin)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == dto.Id && (esAdmin || c.UsuarioId == usuarioId));

        if (cliente is null)
            throw new KeyNotFoundException("Cliente no encontrado");

        cliente.Nombre = dto.Nombre;
        cliente.Telefono = dto.Telefono;
        cliente.Email = dto.Email;

        await _context.SaveChangesAsync();

        return new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Telefono = cliente.Telefono,
            Email = cliente.Email
        };
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId, bool esAdmin)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && (esAdmin || c.UsuarioId == usuarioId));

        if (cliente is null)
            return false;

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();

        return true;
    }

    //Metodo para crear clientes de prueba y probar la paginación
    public async Task<ClienteDto?> CreateAsync()
    {
        for (int i = 1; i <= 100; i++)
        {
            var cliente = new Cliente
            {
                Nombre = $"Cliente {i}",
                Telefono = $"555-000{i}",
                Email = $"cliente{i}@example.com"
            };
            _context.Clientes.Add(cliente);
        }
        await _context.SaveChangesAsync();
        return null;
    }
}
