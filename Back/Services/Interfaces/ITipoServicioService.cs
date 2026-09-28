using Reservas.Common.DTOs.TipoServicio;

namespace Reservas.Services.Interfaces;

public interface ITipoServicioService
{
    Task<List<TipoServicioDto>> GetAllAsync();
    Task<TipoServicioDto?> GetByIdAsync(int id);
    Task<List<TipoServicioDto>> GetByRecursoAsync(int recursoReservableId);
    Task<TipoServicioDto> CreateAsync(TipoServicioCrearDto dto);
    Task<TipoServicioDto> UpdateAsync(TipoServicioActualizarDto dto);
    Task<bool> DeleteAsync(int id);
}
