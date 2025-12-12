using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CampeonatosController : ControllerBase
{
    private readonly ICampeonatoService _campeonatoService;
    private readonly ILogger<CampeonatosController> _logger;

    public CampeonatosController(
        ICampeonatoService campeonatoService,
        ILogger<CampeonatosController> logger)
    {
        _campeonatoService = campeonatoService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os campeonatos
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CampeonatoResumoDto>), 200)]
    public async Task<ActionResult<List<CampeonatoResumoDto>>> GetAll(
        [FromQuery] int? ano = null,
        [FromQuery] int? idModalidade = null)
    {
        var campeonatos = await _campeonatoService.GetAllAsync(ano, idModalidade);
        return Ok(campeonatos);
    }

    /// <summary>
    /// Busca campeonato por ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CampeonatoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CampeonatoDto>> GetById(int id)
    {
        var campeonato = await _campeonatoService.GetByIdAsync(id);

        if (campeonato == null)
            return NotFound($"Campeonato com ID {id} não encontrado");

        return Ok(campeonato);
    }

    /// <summary>
    /// Cria novo campeonato
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(CampeonatoDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CampeonatoDto>> Create([FromBody] CampeonatoCreateDto dto)
    {
        var campeonato = await _campeonatoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = campeonato.Id }, campeonato);
    }

    /// <summary>
    /// Atualiza campeonato
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(CampeonatoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CampeonatoDto>> Update(int id, [FromBody] CampeonatoUpdateDto dto)
    {
        var campeonato = await _campeonatoService.UpdateAsync(id, dto);
        return Ok(campeonato);
    }

    /// <summary>
    /// Altera status do campeonato
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(CampeonatoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CampeonatoDto>> AlterarStatus(
        int id,
        [FromBody] AlterarStatusDto dto)
    {
        var campeonato = await _campeonatoService.AlterarStatusAsync(id, dto.Status);
        return Ok(campeonato);
    }

    /// <summary>
    /// Deleta campeonato (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _campeonatoService.DeleteAsync(id);
        return NoContent();
    }

    // ==================== PONTUAÇÃO ====================

    /// <summary>
    /// Lista pontuações do campeonato
    /// </summary>
    [HttpGet("{id}/pontuacoes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CampeonatoPontuacaoDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<CampeonatoPontuacaoDto>>> GetPontuacoes(int id)
    {
        var pontuacoes = await _campeonatoService.GetPontuacoesAsync(id);
        return Ok(pontuacoes);
    }

    /// <summary>
    /// Define/Atualiza pontuações do campeonato
    /// </summary>
    /// <remarks>
    /// Substitui todas as pontuações existentes pelas novas.
    /// Envie uma lista de posições e pontos.
    /// Exemplo: [{"posicao": 1, "pontos": 25}, {"posicao": 2, "pontos": 22}, ...]
    /// </remarks>
    [HttpPost("{id}/pontuacoes")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(List<CampeonatoPontuacaoDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<CampeonatoPontuacaoDto>>> SetPontuacoes(
        int id,
        [FromBody] List<CampeonatoPontuacaoCreateDto> pontuacoes)
    {
        var result = await _campeonatoService.SetPontuacoesAsync(id, pontuacoes);
        return Ok(result);
    }

    /// <summary>
    /// Aplica template de pontuação padrão
    /// </summary>
    /// <param name="id">ID do campeonato</param>
    /// <param name="template">Template: top10, top15, top20</param>
    [HttpPost("{id}/pontuacoes/template/{template}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(List<CampeonatoPontuacaoDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<CampeonatoPontuacaoDto>>> ApplyTemplate(
        int id,
        string template)
    {
        var result = await _campeonatoService.ApplyPontuacaoTemplateAsync(id, template);
        return Ok(result);
    }

    // ==================== CLASSIFICAÇÃO ====================

    /// <summary>
    /// Classificação geral do campeonato
    /// </summary>
    [HttpGet("{id}/classificacao")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassificacaoCampeonatoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ClassificacaoCampeonatoDto>> GetClassificacao(int id)
    {
        var classificacao = await _campeonatoService.GetClassificacaoAsync(id);
        return Ok(classificacao);
    }

    /// <summary>
    /// Classificação de uma categoria específica
    /// </summary>
    [HttpGet("{id}/classificacao/categoria/{idCategoria}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClassificacaoCategoriaCampeonatoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ClassificacaoCategoriaCampeonatoDto>> GetClassificacaoCategoria(
        int id,
        int idCategoria)
    {
        var classificacao = await _campeonatoService.GetClassificacaoCategoriaAsync(id, idCategoria);
        return Ok(classificacao);
    }

    // ==================== EVENTOS DO CAMPEONATO ====================

    /// <summary>
    /// Lista eventos do campeonato
    /// </summary>
    [HttpGet("{id}/eventos")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventoResumoDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<EventoResumoDto>>> GetEventos(int id)
    {
        var eventos = await _campeonatoService.GetEventosAsync(id);
        return Ok(eventos);
    }

    /// <summary>
    /// Vincula evento ao campeonato
    /// </summary>
    [HttpPost("{id}/eventos/{idEvento}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> VincularEvento(int id, int idEvento)
    {
        await _campeonatoService.VincularEventoAsync(id, idEvento);
        return Ok(new { message = "Evento vinculado ao campeonato com sucesso" });
    }

    /// <summary>
    /// Desvincula evento do campeonato
    /// </summary>
    [HttpDelete("{id}/eventos/{idEvento}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DesvincularEvento(int id, int idEvento)
    {
        await _campeonatoService.DesvincularEventoAsync(id, idEvento);
        return Ok(new { message = "Evento desvinculado do campeonato" });
    }
}
