using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Services.Interfaces;

public interface IResultadoEnduroService
{
    // Classificação completa
    Task<ClassificacaoGeralEnduroDto> CalcularClassificacaoGeralAsync(
        int idEtapa,
        ResultadoFilterParams? filtros = null);

    // Classificação por categoria
    Task<ClassificacaoCategoriaEnduroDto> CalcularClassificacaoCategoriaAsync(
        int idEtapa,
        int idCategoria);

    // Resultado individual
    Task<ResultadoPilotoEnduroDto> CalcularResultadoPilotoAsync(
        int idEtapa,
        int numeroMoto);

    // Resumo rápido (top N)
    Task<List<ResumoClassificacaoDto>> GetResumoClassificacaoAsync(
        int idEtapa,
        int topN = 10,
        int? idCategoria = null);

    // Rankings de especiais
    Task<RankingEspecialDto> GetRankingEspecialAsync(
        int idEtapa,
        int idEspecial,
        int volta);

    Task<List<RankingEspecialDto>> GetTodosRankingsEspeciaisAsync(int idEtapa);

    // Comparativo
    Task<ComparativoPilotosDto> CompararPilotosAsync(
        int idEtapa,
        int numeroMoto1,
        int numeroMoto2);

    // Cache e recálculo
    Task RecalcularResultadosAsync(int idEtapa);
    Task InvalidarCacheAsync(int idEtapa);

}
