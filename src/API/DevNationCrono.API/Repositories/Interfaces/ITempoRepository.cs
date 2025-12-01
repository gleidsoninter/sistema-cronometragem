using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface ITempoRepository
{
    // Buscar
    Task<Tempo?> GetByIdAsync(long id);
    Task<List<Tempo>> GetByEtapaAsync(int idEtapa);
    Task<List<Tempo>> GetByEtapaEVoltaAsync(int idEtapa, int volta);
    Task<List<Tempo>> GetByNumeroMotoEtapaAsync(int numeroMoto, int idEtapa);
    Task<List<Tempo>> GetByInscricaoAsync(int idInscricao);
    Task<PagedResult<Tempo>> GetPagedAsync(LeituraFilterParams filter);

    // Para cálculo ENDURO
    Task<Tempo?> GetEntradaEspecialAsync(int idEtapa, int numeroMoto, int idEspecial, int volta);
    Task<Tempo?> GetSaidaEspecialAsync(int idEtapa, int numeroMoto, int idEspecial, int volta);
    Task<List<Tempo>> GetTemposEspecialAsync(int idEtapa, int idEspecial, int volta);

    // Para cálculo CIRCUITO
    Task<Tempo?> GetUltimaPassagemAsync(int idEtapa, int numeroMoto);
    Task<List<Tempo>> GetPassagensAsync(int idEtapa, int numeroMoto);
    Task<int> GetTotalVoltasAsync(int idEtapa, int numeroMoto);

    // Verificações
    Task<bool> ExisteLeituraAsync(string hashLeitura);
    Task<bool> ExisteLeituraSimilarAsync(int idEtapa, int numeroMoto, DateTime timestamp, string tipo, int toleranciaMs = 1000);

    // CRUD
    Task<Tempo> AddAsync(Tempo tempo);
    Task AddRangeAsync(List<Tempo> tempos);
    Task UpdateAsync(Tempo tempo);
    Task UpdateRangeAsync(List<Tempo> tempos);

    // Estatísticas
    Task<int> CountByEtapaAsync(int idEtapa);
    Task<int> CountByDispositivoAsync(int idDispositivo);
    Task<DateTime?> GetUltimaLeituraEtapaAsync(int idEtapa);
    Task<List<Tempo>> GetByEtapaParaResultadoAsync(int idEtapa);
    Task<Dictionary<int, List<Tempo>>> GetTemposAgrupadosPorMotoAsync(int idEtapa);
    Task<List<ResumoTempoDto>> GetResumoTemposAsync(int idEtapa);
}
