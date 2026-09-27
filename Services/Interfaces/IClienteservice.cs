using Reservas.Common.DTOs.Cliente;
using Reservas.Common.Paginacion;

namespace Reservas.Services.Interfaces;

public interface IClienteService
{
    Task<PagedResult<ClienteDto>> GetAllAsync(int usuarioId, bool esAdmin, int pagina = 1, int cantidad = 10);
    Task<ClienteDto?> GetByIdAsync(int id, int usuarioId, bool esAdmin);
    Task<ClienteDto?> GetByEmailAsync(string email, int usuarioId, bool esAdmin);
    Task<ClienteDto> CreateAsync(ClienteCrearDto dto, int usuarioId, bool esAdmin);
    Task<ClienteDto?> CreateAsync();
    Task<ClienteDto> UpdateAsync(ClienteActualizarDto dto, int usuarioId, bool esAdmin);
    Task<bool> DeleteAsync(int id, int usuarioId, bool esAdmin);
}
