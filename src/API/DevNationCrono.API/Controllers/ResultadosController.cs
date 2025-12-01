using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ResultadosController : ControllerBase
{
    private readonly IResultadoEnduroService _resultadoEnduroService;
    private readonly IEtapaRepository _etapaRepository;
    private readonly ILogger<ResultadosController> _logger;

    public ResultadosController(
        IResultadoEnduroService resultadoEnduroService,
        IEtapaRepository etapaRepository,
        ILogger<ResultadosController> logger)
    {
        _resultadoEnduroService = resultadoEnduroService;
        _etapaRepository = etapaRepository;
        _logger = logger;
    }

    #region Classificação Geral

    /// <summary>
    /// Retorna classificação geral completa de uma etapa de ENDURO
    /// </summary>
    [HttpGet("etapa/{idEtapa}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassificacaoGeralEnduroDto), 200)]
    public async Task<ActionResult<ClassificacaoGeralEnduroDto>> GetClassificacaoGeral(
        int idEtapa,
        [FromQuery] int? idCategoria = null,
        [FromQuery] bool incluirDesclassificados = false,
        [FromQuery] bool incluirDetalhes = true)
    {
        var filtros = new ResultadoFilterParams
        {
            IdCategoria = idCategoria,
            IncluirDesclassificados = incluirDesclassificados,
            IncluirDetalhes = incluirDetalhes
        };

        var classificacao = await _resultadoEnduroService.CalcularClassificacaoGeralAsync(idEtapa, filtros);
        return Ok(classificacao);
    }

    /// <summary>
    /// Retorna resumo da classificação (top N)
    /// </summary>
    [HttpGet("etapa/{idEtapa}/resumo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ResumoClassificacaoDto>), 200)]
    public async Task<ActionResult<List<ResumoClassificacaoDto>>> GetResumo(
        int idEtapa,
        [FromQuery] int topN = 10,
        [FromQuery] int? idCategoria = null)
    {
        var resumo = await _resultadoEnduroService.GetResumoClassificacaoAsync(idEtapa, topN, idCategoria);
        return Ok(resumo);
    }

    #endregion

    #region Por Categoria

    /// <summary>
    /// Retorna classificação de uma categoria específica
    /// </summary>
    [HttpGet("etapa/{idEtapa}/categoria/{idCategoria}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassificacaoCategoriaEnduroDto), 200)]
    public async Task<ActionResult<ClassificacaoCategoriaEnduroDto>> GetClassificacaoCategoria(
        int idEtapa,
        int idCategoria)
    {
        var classificacao = await _resultadoEnduroService.CalcularClassificacaoCategoriaAsync(idEtapa, idCategoria);
        return Ok(classificacao);
    }

    #endregion

    #region Por Piloto

    /// <summary>
    /// Retorna resultado detalhado de um piloto
    /// </summary>
    [HttpGet("etapa/{idEtapa}/moto/{numeroMoto}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultadoPilotoEnduroDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ResultadoPilotoEnduroDto>> GetResultadoPiloto(
        int idEtapa,
        int numeroMoto)
    {
        var resultado = await _resultadoEnduroService.CalcularResultadoPilotoAsync(idEtapa, numeroMoto);
        return Ok(resultado);
    }

    /// <summary>
    /// Compara dois pilotos
    /// </summary>
    [HttpGet("etapa/{idEtapa}/comparar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ComparativoPilotosDto), 200)]
    public async Task<ActionResult<ComparativoPilotosDto>> CompararPilotos(
        int idEtapa,
        [FromQuery] int moto1,
        [FromQuery] int moto2)
    {
        var comparativo = await _resultadoEnduroService.CompararPilotosAsync(idEtapa, moto1, moto2);
        return Ok(comparativo);
    }

    #endregion

    #region Rankings de Especiais

    /// <summary>
    /// Retorna ranking de uma especial específica
    /// </summary>
    [HttpGet("etapa/{idEtapa}/especial/{idEspecial}/volta/{volta}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RankingEspecialDto), 200)]
    public async Task<ActionResult<RankingEspecialDto>> GetRankingEspecial(
        int idEtapa,
        int idEspecial,
        int volta)
    {
        var ranking = await _resultadoEnduroService.GetRankingEspecialAsync(idEtapa, idEspecial, volta);
        return Ok(ranking);
    }

    /// <summary>
    /// Retorna todos os rankings de especiais
    /// </summary>
    [HttpGet("etapa/{idEtapa}/especiais")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<RankingEspecialDto>), 200)]
    public async Task<ActionResult<List<RankingEspecialDto>>> GetTodosRankings(int idEtapa)
    {
        var rankings = await _resultadoEnduroService.GetTodosRankingsEspeciaisAsync(idEtapa);
        return Ok(rankings);
    }

    #endregion

    #region Recálculo

    /// <summary>
    /// Força recálculo de todos os resultados
    /// </summary>
    [HttpPost("etapa/{idEtapa}/recalcular")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Recalcular(int idEtapa)
    {
        await _resultadoEnduroService.RecalcularResultadosAsync(idEtapa);
        return Ok(new { mensagem = "Resultados recalculados com sucesso" });
    }

    /// <summary>
    /// Invalida cache de resultados
    /// </summary>
    [HttpPost("etapa/{idEtapa}/invalidar-cache")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> InvalidarCache(int idEtapa)
    {
        await _resultadoEnduroService.InvalidarCacheAsync(idEtapa);
        return Ok(new { mensagem = "Cache invalidado" });
    }

    #endregion
}
