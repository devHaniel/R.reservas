using Reservas.Common.DTOs.Reserva;

namespace Reservas.Services.Interfaces;

public interface IReservaService
{
    Task<List<ReservaDto>> GetAllAsync(int usuarioId, bool esAdmin);
    Task<ReservaDto?> GetByIdAsync(int id, int usuarioId, bool esAdmin);
    Task<List<ReservaDto>> GetByClienteAsync(int clienteId, int usuarioId, bool esAdmin);
    Task<List<ReservaDto>> GetByRecursoAsync(int recursoReservableId, int usuarioId, bool esAdmin);
    Task<ReservaDto> CreateAsync(ReservaCrearDto dto, int usuarioId, bool esAdmin);
    Task<ReservaDto> UpdateAsync(ReservaActualizarDto dto, int usuarioId, bool esAdmin);
    Task<bool> DeleteAsync(int id, int usuarioId, bool esAdmin);
}
