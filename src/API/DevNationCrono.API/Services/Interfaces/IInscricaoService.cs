using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Services.Interfaces;

public interface IInscricaoService
{
    // Buscar
    Task<InscricaoDto?> GetByIdAsync(int id);
    Task<List<InscricaoDto>> GetByEventoAsync(int idEvento);
    Task<List<InscricaoDto>> GetByEtapaAsync(int idEtapa);
    Task<List<InscricaoDto>> GetByCategoriaAsync(int idCategoria);
    Task<List<InscricaoDto>> GetByPilotoAsync(int idPiloto);
    Task<PagedResult<InscricaoResumoDto>> GetPagedAsync(InscricaoFilterParams filter);

    // Inscrever
    Task<InscricaoDto> InscreverAsync(InscricaoCreateDto dto);
    Task<InscricaoMultiplaResponseDto> InscreverMultiplasCategoriasAsync(InscricaoMultiplaCreateDto dto);

    // Simular valores
    Task<SimulacaoInscricaoResponseDto> SimularValoresAsync(SimularInscricaoDto dto);

    // Atualizar
    Task<InscricaoDto> UpdateAsync(int id, InscricaoUpdateDto dto);

    // Pagamento
    Task<InscricaoDto> ConfirmarPagamentoAsync(int id, ConfirmarPagamentoDto dto);
    Task<InscricaoDto> CancelarInscricaoAsync(int id, string? motivo = null);
    Task<string> GerarQrCodePixAsync(int idInscricao);
    Task<List<string>> GerarQrCodePixMultiplasAsync(List<int> idsInscricoes);

    // Número de moto
    Task<bool> ValidarNumeroMotoAsync(int numeroMoto, int idEvento, int? excludeId = null);
    Task<InscricaoDto> AlterarNumeroMotoAsync(int id, int novoNumero);

    // Estatísticas
    Task<EstatisticasInscricaoDto> GetEstatisticasEventoAsync(int idEvento);
}
