using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface ICategoriaRepository
{
    Task<Categoria?> GetByIdAsync(int id);
    Task<List<Categoria>> GetByEventoAsync(int idEvento);
    Task<List<Categoria>> GetActivesByEventoAsync(int idEvento);
    Task<Categoria> AddAsync(Categoria categoria);
    Task UpdateAsync(Categoria categoria);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> NomeExistsNoEventoAsync(string nome, int idEvento, int? excludeId = null);
    Task<int> CountInscritosAsync(int id);
}
