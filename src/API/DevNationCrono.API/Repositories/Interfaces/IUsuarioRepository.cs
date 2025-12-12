using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByEmailAsync(string email);
    Task<List<Usuario>> GetAllAsync();
    Task<List<Usuario>> GetByRoleAsync(string role);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<Usuario> AddAsync(Usuario usuario);
    Task<Usuario> UpdateAsync(Usuario usuario);
    Task DeleteAsync(int id);
}