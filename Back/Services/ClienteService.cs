using Microsoft.EntityFrameworkCore;
using Reservas.Common.DTOs.Cliente;
using Reservas.Common.Paginacion;
using Reservas.Data;
using Reservas.Models.Entities;
using Reservas.Services.Interfaces;

namespace Reservas.Services;

/// <summary>
/// Los clientes son globales (una sola sede): cualquier Admin o Vendedor los ve.
/// El borrado es lógico para no perder el historial de reservas.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly AppDbContext _context;

    public ClienteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ClienteDto>> GetAllAsync(int pagina = 1, int cantidad = 10)
    {
        var query = _context.Clientes
            .AsNoTracking()
            .Where(c => c.EliminadoEn == null);

        var total = await query.CountAsync();

        var clientes = await query
            .OrderBy(c => c.Id)
            .Skip((pagina - 1) * cantidad)
            .Take(cantidad)
            .ToListAsync();

        return new PagedResult<ClienteDto>
        {
            Items = clientes.Select(Map).ToList(),
            Pagina = pagina,
            Cantidad = cantidad,
            Total = total
        };
    }

    public async Task<ClienteDto?> GetByIdAsync(int id)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.EliminadoEn == null);

        return cliente is null ? null : Map(cliente);
    }

    public async Task<ClienteDto?> GetByEmailAsync(string email)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EliminadoEn == null
                && c.Email != null
                && c.Email.ToUpper() == email.ToUpper());

        return cliente is null ? null : Map(cliente);
    }

    public async Task<ClienteDto> CreateAsync(ClienteCrearDto dto, int usuarioId)
    {
        var cliente = new Cliente
        {
            UsuarioId = usuarioId,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Email = dto.Email
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return Map(cliente);
    }

    public async Task<ClienteDto> UpdateAsync(ClienteActualizarDto dto)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.EliminadoEn == null)
            ?? throw new KeyNotFoundException("Cliente no encontrado");

        cliente.Nombre = dto.Nombre;
        cliente.Telefono = dto.Telefono;
        cliente.Email = dto.Email;

        await _context.SaveChangesAsync();

        return Map(cliente);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.EliminadoEn == null);

        if (cliente is null)
            return false;

        // Borrado lógico: conservamos las reservas históricas.
        cliente.EliminadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private static ClienteDto Map(Cliente c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Telefono = c.Telefono,
        Email = c.Email
    };
}