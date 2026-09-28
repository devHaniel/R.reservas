using Reservas.Common.DTOs.Cliente;
using Reservas.Common.Paginacion;

namespace Reservas.Services.Interfaces;

public interface IClienteService
{
    Task<PagedResult<ClienteDto>> GetAllAsync(int pagina = 1, int cantidad = 10);
    Task<ClienteDto?> GetByIdAsync(int id);
    Task<ClienteDto?> GetByEmailAsync(string email);
    Task<ClienteDto> CreateAsync(ClienteCrearDto dto, int usuarioId);
    Task<ClienteDto> UpdateAsync(ClienteActualizarDto dto);
    Task<bool> DeleteAsync(int id);
}