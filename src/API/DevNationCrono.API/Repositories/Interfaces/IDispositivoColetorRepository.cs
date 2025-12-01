using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IDispositivoColetorRepository
{
    Task<DispositivoColetor?> GetByIdAsync(int id);
    Task<DispositivoColetor?> GetByDeviceIdAsync(string deviceId);
    Task<List<DispositivoColetor>> GetByEventoAsync(int idEvento);
    Task<DispositivoColetor> AddAsync(DispositivoColetor coletor);
    Task UpdateAsync(DispositivoColetor coletor);
    Task<bool> DeviceIdExistsAsync(string deviceId);
}
