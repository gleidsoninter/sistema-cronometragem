using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IDispositivoColetorRepository
{
    Task<DispositivoColetor?> GetByIdAsync(int id);

    Task<DispositivoColetor?> GetByDeviceIdAsync(string deviceId);

    Task<DispositivoColetor?> GetByDeviceIdEtapaAsync(string deviceId, int idEtapa);

    Task<List<DispositivoColetor>> GetByEtapaAsync(int idEtapa);

    Task<List<DispositivoColetor>> GetByEventoAsync(int idEvento);

    Task<DispositivoColetor> AddAsync(DispositivoColetor dispositivo);

    Task UpdateAsync(DispositivoColetor dispositivo);

    Task<bool> ExisteDeviceIdAsync(string deviceId, int? excludeId = null);

    Task AtualizarStatusConexaoAsync(int id, string status);

    Task IncrementarLeiturasAsync(int id);
}
