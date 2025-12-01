using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class LeiturasController : ControllerBase
{
    private readonly ICronometragemService _cronometragemService;
    private readonly ILogger<LeiturasController> _logger;

    public LeiturasController(
        ICronometragemService cronometragemService,
        ILogger<LeiturasController> logger)
    {
        _cronometragemService = cronometragemService;
        _logger = logger;
    }

    #region Receber Leituras

    /// <summary>
    /// Recebe uma leitura individual do coletor
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Coletor,Admin,Organizador")]
    [ProducesResponseType(typeof(LeituraResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<LeituraResponseDto>> ReceberLeitura([FromBody] LeituraDto leitura)
    {
        var resultado = await _cronometragemService.ProcessarLeituraAsync(leitura);

        if (resultado.Status == "ERRO")
        {
            return BadRequest(resultado);
        }

        return Ok(resultado);
    }

    /// <summary>
    /// Recebe lote de leituras (sincronização offline)
    /// </summary>
    [HttpPost("lote")]
    [Authorize(Roles = "Coletor,Admin,Organizador")]
    [ProducesResponseType(typeof(LoteLeituraResponseDto), 200)]
    public async Task<ActionResult<LoteLeituraResponseDto>> ReceberLote([FromBody] LoteLeituraDto lote)
    {
        var resultado = await _cronometragemService.ProcessarLoteLeituraAsync(lote);
        return Ok(resultado);
    }

    #endregion

    #region Consultas

    /// <summary>
    /// Lista leituras com paginação e filtros
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<LeituraResponseDto>), 200)]
    public async Task<ActionResult<PagedResult<LeituraResponseDto>>> GetPaged(
        [FromQuery] LeituraFilterParams filter)
    {
        var resultado = await _cronometragemService.GetLeiturasPagedAsync(filter);
        return Ok(resultado);
    }

    /// <summary>
    /// Lista todas as leituras de uma etapa
    /// </summary>
    [HttpGet("etapa/{idEtapa}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<LeituraResponseDto>), 200)]
    public async Task<ActionResult<List<LeituraResponseDto>>> GetByEtapa(int idEtapa)
    {
        var leituras = await _cronometragemService.GetLeiturasEtapaAsync(idEtapa);
        return Ok(leituras);
    }

    /// <summary>
    /// Lista tempos de um piloto específico
    /// </summary>
    [HttpGet("etapa/{idEtapa}/moto/{numeroMoto}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TempoCalculadoDto>), 200)]
    public async Task<ActionResult<List<TempoCalculadoDto>>> GetTemposPiloto(
        int idEtapa, int numeroMoto)
    {
        var tempos = await _cronometragemService.GetTemposPilotoAsync(idEtapa, numeroMoto);
        return Ok(tempos);
    }

    #endregion

    #region Correções

    /// <summary>
    /// Corrige uma leitura manualmente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(LeituraResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LeituraResponseDto>> Corrigir(
        long id,
        [FromBody] CorrecaoTempoDto correcao)
    {
        var resultado = await _cronometragemService.CorrigirLeituraAsync(id, correcao);
        return Ok(resultado);
    }

    /// <summary>
    /// Descarta uma leitura (erro de digitação, etc.)
    /// </summary>
    [HttpPost("{id}/descartar")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(LeituraResponseDto), 200)]
    public async Task<ActionResult<LeituraResponseDto>> Descartar(
        long id,
        [FromBody] DescartarLeituraDto dto)
    {
        var resultado = await _cronometragemService.DescartarLeituraAsync(id, dto.Motivo);
        return Ok(resultado);
    }

    /// <summary>
    /// Restaura uma leitura descartada
    /// </summary>
    [HttpPost("{id}/restaurar")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(LeituraResponseDto), 200)]
    public async Task<ActionResult<LeituraResponseDto>> Restaurar(long id)
    {
        var resultado = await _cronometragemService.RestaurarLeituraAsync(id);
        return Ok(resultado);
    }

    /// <summary>
    /// Recalcula todos os tempos de uma etapa
    /// </summary>
    [HttpPost("etapa/{idEtapa}/recalcular")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> RecalcularEtapa(int idEtapa)
    {
        await _cronometragemService.RecalcularTemposEtapaAsync(idEtapa);
        return Ok(new { mensagem = "Tempos recalculados com sucesso" });
    }

    #endregion
}

public class DescartarLeituraDto
{
    public string Motivo { get; set; }
}
