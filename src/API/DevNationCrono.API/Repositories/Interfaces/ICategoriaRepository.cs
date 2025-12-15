using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface ICategoriaRepository
{
    Task<Categoria?> GetByIdAsync(int id);
    Task<List<Categoria>> GetByModalidadeAsync(int idModalidade);
    Task<List<Categoria>> GetActivesByModalidadeAsync(int idModalidade);
    Task<Categoria> AddAsync(Categoria categoria);
    Task UpdateAsync(Categoria categoria);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> NomeExistsNaModalidadeAsync(string nome, int idModalidade, int? excludeId = null);
    Task<int> CountInscritosAsync(int id);
    Task<List<Categoria>> GetByEtapaAsync(int idEtapa);
}
