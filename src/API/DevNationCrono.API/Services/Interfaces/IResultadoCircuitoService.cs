using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface IResultadoCircuitoService
{
    // Classificação
    Task<ClassificacaoGeralCircuitoDto> CalcularClassificacaoGeralAsync(int idEtapa);
    Task<ClassificacaoCategoriaCircuitoDto> CalcularClassificacaoCategoriaAsync(int idEtapa, int idCategoria);
    Task<ResultadoPilotoCircuitoDto> GetResultadoPilotoAsync(int idEtapa, int numeroMoto);

    // Tempo real (otimizado para atualizações frequentes)
    Task<List<ResumoTempoRealDto>> GetResumoTempoRealAsync(int idEtapa, int? idCategoria = null);
    Task<List<PassagemRecente>> GetUltimasPassagensAsync(int idEtapa, int quantidade = 10);

    // Análise
    Task<AnaliseDesempenhoDto> GetAnaliseDesempenhoAsync(int idEtapa, int numeroMoto);
    Task<List<AnaliseDesempenhoDto>> GetRankingMelhorVoltaAsync(int idEtapa, int? idCategoria = null);

    // Controle da prova
    Task<ControleProvaDto> GetStatusProvaAsync(int idEtapa);
    Task<ControleProvaDto> IniciarProvaAsync(IniciarProvaDto dto);
    Task<ControleProvaDto> DarBandeiraAsync(EncerrarProvaDto dto);
    Task<ControleProvaDto> FinalizarProvaAsync(int idEtapa);

    // Atualização incremental (chamado quando nova passagem é registrada)
    Task AtualizarResultadoIncrementalAsync(int idEtapa, int numeroMoto);

    // Cache
    Task InvalidarCacheAsync(int idEtapa);
}