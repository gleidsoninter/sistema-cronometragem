using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resultados/circuito")]
public class ResultadosCircuitoController : ControllerBase
{
    private readonly IResultadoCircuitoService _resultadoService;
    private readonly ILogger<ResultadosCircuitoController> _logger;

    public ResultadosCircuitoController(
        IResultadoCircuitoService resultadoService,
        ILogger<ResultadosCircuitoController> logger)
    {
        _resultadoService = resultadoService;
        _logger = logger;
    }

    #region Classificação

    /// <summary>
    /// Retorna classificação geral completa de uma etapa de CIRCUITO
    /// </summary>
    [HttpGet("etapa/{idEtapa}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassificacaoGeralCircuitoDto), 200)]
    public async Task<ActionResult<ClassificacaoGeralCircuitoDto>> GetClassificacaoGeral(int idEtapa)
    {
        var classificacao = await _resultadoService.CalcularClassificacaoGeralAsync(idEtapa);
        return Ok(classificacao);
    }

    /// <summary>
    /// Retorna classificação de uma categoria específica
    /// </summary>
    [HttpGet("etapa/{idEtapa}/categoria/{idCategoria}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassificacaoCategoriaCircuitoDto), 200)]
    public async Task<ActionResult<ClassificacaoCategoriaCircuitoDto>> GetClassificacaoCategoria(
        int idEtapa,
        int idCategoria)
    {
        var classificacao = await _resultadoService.CalcularClassificacaoCategoriaAsync(idEtapa, idCategoria);
        return Ok(classificacao);
    }

    /// <summary>
    /// Retorna resultado detalhado de um piloto
    /// </summary>
    [HttpGet("etapa/{idEtapa}/moto/{numeroMoto}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultadoPilotoCircuitoDto), 200)]
    public async Task<ActionResult<ResultadoPilotoCircuitoDto>> GetResultadoPiloto(
        int idEtapa,
        int numeroMoto)
    {
        var resultado = await _resultadoService.GetResultadoPilotoAsync(idEtapa, numeroMoto);
        return Ok(resultado);
    }

    #endregion

    #region Tempo Real

    /// <summary>
    /// Retorna resumo para exibição em tempo real (leve e rápido)
    /// </summary>
    [HttpGet("etapa/{idEtapa}/tempo-real")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ResumoTempoRealDto>), 200)]
    public async Task<ActionResult<List<ResumoTempoRealDto>>> GetTempoReal(
        int idEtapa,
        [FromQuery] int? idCategoria = null)
    {
        var resumo = await _resultadoService.GetResumoTempoRealAsync(idEtapa, idCategoria);
        return Ok(resumo);
    }

    /// <summary>
    /// Retorna últimas passagens registradas
    /// </summary>
    [HttpGet("etapa/{idEtapa}/passagens")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PassagemRecente>), 200)]
    public async Task<ActionResult<List<PassagemRecente>>> GetUltimasPassagens(
        int idEtapa,
        [FromQuery] int quantidade = 10)
    {
        var passagens = await _resultadoService.GetUltimasPassagensAsync(idEtapa, quantidade);
        return Ok(passagens);
    }

    #endregion

    #region Análise

    /// <summary>
    /// Retorna análise de desempenho de um piloto
    /// </summary>
    [HttpGet("etapa/{idEtapa}/moto/{numeroMoto}/analise")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AnaliseDesempenhoDto), 200)]
    public async Task<ActionResult<AnaliseDesempenhoDto>> GetAnaliseDesempenho(
        int idEtapa,
        int numeroMoto)
    {
        var analise = await _resultadoService.GetAnaliseDesempenhoAsync(idEtapa, numeroMoto);
        return Ok(analise);
    }

    /// <summary>
    /// Retorna ranking de melhor volta
    /// </summary>
    [HttpGet("etapa/{idEtapa}/melhor-volta")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<AnaliseDesempenhoDto>), 200)]
    public async Task<ActionResult<List<AnaliseDesempenhoDto>>> GetRankingMelhorVolta(
        int idEtapa,
        [FromQuery] int? idCategoria = null)
    {
        var ranking = await _resultadoService.GetRankingMelhorVoltaAsync(idEtapa, idCategoria);
        return Ok(ranking);
    }

    #endregion

    #region Controle da Prova

    /// <summary>
    /// Retorna status atual da prova
    /// </summary>
    [HttpGet("etapa/{idEtapa}/status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ControleProvaDto), 200)]
    public async Task<ActionResult<ControleProvaDto>> GetStatusProva(int idEtapa)
    {
        var status = await _resultadoService.GetStatusProvaAsync(idEtapa);
        return Ok(status);
    }

    /// <summary>
    /// Inicia a prova (registra hora da largada)
    /// </summary>
    [HttpPost("etapa/{idEtapa}/iniciar")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(ControleProvaDto), 200)]
    public async Task<ActionResult<ControleProvaDto>> IniciarProva(
        int idEtapa,
        [FromBody] IniciarProvaDto? dto = null)
    {
        var request = dto ?? new IniciarProvaDto { IdEtapa = idEtapa };
        request.IdEtapa = idEtapa;

        var status = await _resultadoService.IniciarProvaAsync(request);
        return Ok(status);
    }

    /// <summary>
    /// Dá a bandeira quadriculada (fim do tempo)
    /// </summary>
    [HttpPost("etapa/{idEtapa}/bandeira")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(ControleProvaDto), 200)]
    public async Task<ActionResult<ControleProvaDto>> DarBandeira(
        int idEtapa,
        [FromBody] EncerrarProvaDto? dto = null)
    {
        var request = dto ?? new EncerrarProvaDto { IdEtapa = idEtapa };
        request.IdEtapa = idEtapa;

        var status = await _resultadoService.DarBandeiraAsync(request);
        return Ok(status);
    }

    /// <summary>
    /// Finaliza a prova (todos cruzaram a linha)
    /// </summary>
    [HttpPost("etapa/{idEtapa}/finalizar")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(ControleProvaDto), 200)]
    public async Task<ActionResult<ControleProvaDto>> FinalizarProva(int idEtapa)
    {
        var status = await _resultadoService.FinalizarProvaAsync(idEtapa);
        return Ok(status);
    }

    #endregion

    #region Cache

    /// <summary>
    /// Invalida cache de resultados
    /// </summary>
    [HttpPost("etapa/{idEtapa}/invalidar-cache")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> InvalidarCache(int idEtapa)
    {
        await _resultadoService.InvalidarCacheAsync(idEtapa);
        return Ok(new { mensagem = "Cache invalidado" });
    }

    #endregion
}
