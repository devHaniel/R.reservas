using Reservas.Common.DTOs.Reserva;

namespace Reservas.Services.Interfaces;

public interface IReservaService
{
    Task<List<ReservaDto>> GetAllAsync();
    Task<ReservaDto?> GetByIdAsync(int id);
    Task<List<ReservaDto>> GetByClienteAsync(int clienteId);
    Task<List<ReservaDto>> GetByRecursoAsync(int recursoReservableId);
    Task<ReservaDto> CreateAsync(ReservaCrearDto dto);
    Task<ReservaDto> UpdateAsync(ReservaActualizarDto dto);
    Task<bool> DeleteAsync(int id);
}
