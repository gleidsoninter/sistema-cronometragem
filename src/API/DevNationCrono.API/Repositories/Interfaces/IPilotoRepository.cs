using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IPilotoRepository
{
    Task<Piloto?> GetByIdAsync(int id);
    Task<Piloto?> GetByCpfAsync(string cpf);
    Task<Piloto?> GetByEmailAsync(string email);
    Task<List<Piloto>> GetAllAsync();
    Task<List<Piloto>> GetActivesAsync();
    Task<Piloto> AddAsync(Piloto piloto);
    Task UpdateAsync(Piloto piloto);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> CpfExistsAsync(string cpf);
    Task<bool> EmailExistsAsync(string email);
    Task<PagedResult<Piloto>> GetPagedAsync(PilotoFilterParams filterParams);
}
