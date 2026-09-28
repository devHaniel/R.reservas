using Reservas.Common.DTOs.Reserva;
using Reservas.Models.Entities;

namespace Reservas.Services.Interfaces;

public interface IReservaService
{
    Task<List<ReservaDto>> GetAllAsync();
    Task<ReservaDto?> GetByIdAsync(int id);
    Task<List<ReservaDto>> GetByClienteAsync(int clienteId);
    Task<List<ReservaDto>> GetByRecursoAsync(int recursoReservableId);
    Task<List<ReservaDto>> GetByRangoAsync(string? desde, string? hasta, int? recursoReservableId);
    Task<ReservaDto> CreateAsync(ReservaCrearDto dto);
    Task<ReservaDto> UpdateAsync(ReservaActualizarDto dto);
    Task<ReservaDto?> CambiarEstadoAsync(int id, EstadoReserva estado);
    Task<bool> DeleteAsync(int id);
}