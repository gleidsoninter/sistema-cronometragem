using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IEventoRepository
{
    Task<Evento?> GetByIdAsync(int id);
    Task<Evento?> GetByIdWithDetailsAsync(int id);
    Task<List<Evento>> GetAllAsync();
    Task<List<Evento>> GetActivesAsync();
    Task<PagedResult<Evento>> GetPagedAsync(EventoFilterParams filter);
    Task<List<Evento>> GetByModalidadeAsync(int idModalidade);
    Task<List<Evento>> GetProximosAsync(int quantidade = 5);
    Task<Evento> AddAsync(Evento evento);
    Task UpdateAsync(Evento evento);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> CountEtapasAsync(int id);
    Task<int> CountCategoriasAsync(int id);
    Task<int> CountInscritosAsync(int id);
}
