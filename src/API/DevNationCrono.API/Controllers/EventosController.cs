using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class EventosController : ControllerBase
{
    private readonly IEventoService _eventoService;
    private readonly ILogger<EventosController> _logger;

    public EventosController(
        IEventoService eventoService,
        ILogger<EventosController> logger)
    {
        _eventoService = eventoService;
        _logger = logger;
    }

    /// <summary>
    /// Lista eventos com paginação e filtros
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<EventoResumoDto>), 200)]
    public async Task<ActionResult<PagedResult<EventoResumoDto>>> GetPaged(
        [FromQuery] EventoFilterParams filter)
    {
        var result = await _eventoService.GetPagedAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lista eventos ativos
    /// </summary>
    [HttpGet("ativos")]
    [MapToApiVersion("1.0")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventoResumoDto>), 200)]
    public async Task<ActionResult<List<EventoResumoDto>>> GetActives()
    {
        var eventos = await _eventoService.GetActivesAsync();
        return Ok(eventos);
    }

    /// <summary>
    /// Lista próximos eventos
    /// </summary>
    [HttpGet("proximos")]
    [AllowAnonymous]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<EventoResumoDto>), 200)]
    public async Task<ActionResult<List<EventoResumoDto>>> GetProximos(
        [FromQuery] int quantidade = 5)
    {
        var eventos = await _eventoService.GetProximosAsync(quantidade);
        return Ok(eventos);
    }

    /// <summary>
    /// Busca evento por ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EventoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventoDto>> GetById(int id)
    {
        var evento = await _eventoService.GetByIdAsync(id);

        if (evento == null)
            return NotFound($"Evento com ID {id} não encontrado");

        return Ok(evento);
    }

    /// <summary>
    /// Cria novo evento
    /// </summary>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EventoDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<EventoDto>> Create([FromBody] EventoCreateDto dto)
    {
        var evento = await _eventoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = evento.Id }, evento);
    }

    /// <summary>
    /// Atualiza evento
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion("1.0")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EventoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventoDto>> Update(int id, [FromBody] EventoUpdateDto dto)
    {
        var evento = await _eventoService.UpdateAsync(id, dto);
        return Ok(evento);
    }

    /// <summary>
    /// Abre inscrições do evento
    /// </summary>
    [HttpPost("{id}/abrir-inscricoes")]
    [MapToApiVersion("1.0")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EventoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventoDto>> AbrirInscricoes(int id)
    {
        var evento = await _eventoService.AbrirInscricoesAsync(id);
        return Ok(evento);
    }

    /// <summary>
    /// Fecha inscrições do evento
    /// </summary>
    [HttpPost("{id}/fechar-inscricoes")]
    [MapToApiVersion("1.0")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EventoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventoDto>> FecharInscricoes(int id)
    {
        var evento = await _eventoService.FecharInscricoesAsync(id);
        return Ok(evento);
    }

    /// <summary>
    /// Altera status do evento
    /// </summary>
    [HttpPatch("{id}/status")]
    [MapToApiVersion("1.0")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EventoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventoDto>> AlterarStatus(
        int id,
        [FromBody] AlterarStatusDto dto)
    {
        var evento = await _eventoService.AlterarStatusAsync(id, dto.Status);
        return Ok(evento);
    }

    /// <summary>
    /// Deleta evento (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _eventoService.DeleteAsync(id);
        return NoContent();
    }
}

public class AlterarStatusDto
{
    [Required]
    public string Status { get; set; }
}
