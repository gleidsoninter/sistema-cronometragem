using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Services.Interfaces;

public interface IEventoService
{
    Task<EventoDto?> GetByIdAsync(int id);
    Task<List<EventoResumoDto>> GetAllAsync();
    Task<List<EventoResumoDto>> GetActivesAsync();
    Task<PagedResult<EventoResumoDto>> GetPagedAsync(EventoFilterParams filter);
    Task<List<EventoResumoDto>> GetProximosAsync(int quantidade = 5);
    Task<EventoDto> CreateAsync(EventoCreateDto dto);
    Task<EventoDto> UpdateAsync(int id, EventoUpdateDto dto);
    Task<EventoDto> AbrirInscricoesAsync(int id);
    Task<EventoDto> FecharInscricoesAsync(int id);
    Task<EventoDto> AlterarStatusAsync(int id, string novoStatus);
    Task DeleteAsync(int id);
}
