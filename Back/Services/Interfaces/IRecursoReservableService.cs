using Reservas.Common.DTOs.RecursoReservable;

namespace Reservas.Services.Interfaces;

public interface IRecursoReservableService
{
    Task<List<RecursoReservableDto>> GetAllAsync();
    Task<RecursoReservableDto?> GetByIdAsync(int id);
    Task<List<RecursoReservableDto>> GetByCategoriaAsync(string categoria);
    Task<RecursoReservableDto> CreateAsync(RecursoReservableCrearDto dto);
    Task<RecursoReservableDto> UpdateAsync(RecursoReservableActualizarDto dto);
    Task<bool> DeleteAsync(int id);
}
