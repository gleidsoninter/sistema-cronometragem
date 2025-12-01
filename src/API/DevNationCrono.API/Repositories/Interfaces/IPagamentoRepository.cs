using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IPagamentoRepository
{
    Task<Pagamento?> GetByIdAsync(int id);
    Task<Pagamento?> GetPendenteByInscricaoAsync(int idInscricao);
    Task<Pagamento?> GetUltimoByInscricaoAsync(int idInscricao);
    Task<List<Pagamento>> GetByIdExternoAsync(string idExterno);
    Task<List<Pagamento>> GetByInscricaoAsync(int idInscricao);
    Task<List<Pagamento>> GetExpiradasAsync();
    Task<Pagamento> AddAsync(Pagamento pagamento);
    Task UpdateAsync(Pagamento pagamento);
}
