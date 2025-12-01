using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IInscricaoRepository
{
    // Buscar
    Task<Inscricao?> GetByIdAsync(int id);
    Task<Inscricao?> GetByIdWithDetailsAsync(int id);
    Task<List<Inscricao>> GetByEventoAsync(int idEvento);
    Task<List<Inscricao>> GetByEtapaAsync(int idEtapa);
    Task<List<Inscricao>> GetByCategoriaAsync(int idCategoria);
    Task<List<Inscricao>> GetByPilotoAsync(int idPiloto);
    Task<List<Inscricao>> GetByPilotoEventoAsync(int idPiloto, int idEvento);
    Task<PagedResult<Inscricao>> GetPagedAsync(InscricaoFilterParams filter);

    // Verificações
    Task<bool> ExistsAsync(int id);
    Task<bool> JaInscritoNaCategoriaAsync(int idPiloto, int idCategoria, int idEtapa);
    Task<bool> JaInscritoNoEventoAsync(int idPiloto, int idEvento);
    Task<int> ContarInscricoesPilotoEventoAsync(int idPiloto, int idEvento);
    Task<bool> NumeroMotoEmUsoAsync(int numeroMoto, int idEvento, int? excludeIdInscricao = null);

    // Número de moto
    Task<int> GetProximoNumeroMotoAsync(int idEvento);
    Task<int?> GetNumeroMotoPilotoEventoAsync(int idPiloto, int idEvento);

    // CRUD
    Task<Inscricao> AddAsync(Inscricao inscricao);
    Task AddRangeAsync(List<Inscricao> inscricoes);
    Task UpdateAsync(Inscricao inscricao);
    Task DeleteAsync(int id);

    // Estatísticas
    Task<int> CountByEventoAsync(int idEvento);
    Task<int> CountByCategoriaAsync(int idCategoria);
    Task<int> CountPagosEventoAsync(int idEvento);
    Task<decimal> SomarValorPagosEventoAsync(int idEvento);
}
