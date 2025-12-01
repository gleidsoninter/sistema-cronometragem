using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IModalidadeRepository
{
    Task<Modalidade?> GetByIdAsync(int id);
    Task<Modalidade?> GetByIdWithEventosAsync(int id);
    Task<Modalidade?> GetByNomeAsync(string nome);
    Task<List<Modalidade>> GetAllAsync();
    Task<List<Modalidade>> GetActivesAsync();
    Task<List<Modalidade>> GetByTipoAsync(string tipoCronometragem);
    Task<Modalidade> AddAsync(Modalidade modalidade);
    Task UpdateAsync(Modalidade modalidade);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> NomeExistsAsync(string nome, int? excludeId = null);
    Task<int> CountEventosAsync(int id);
}
