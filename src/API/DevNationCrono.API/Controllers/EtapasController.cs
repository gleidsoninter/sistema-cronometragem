using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class EtapasController : ControllerBase
{
    private readonly IEtapaService _etapaService;
    private readonly ILogger<EtapasController> _logger;

    public EtapasController(
        IEtapaService etapaService,
        ILogger<EtapasController> logger)
    {
        _etapaService = etapaService;
        _logger = logger;
    }

    /// <summary>
    /// Lista etapas de um evento
    /// </summary>
    [HttpGet("evento/{idEvento}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EtapaDto>), 200)]
    public async Task<ActionResult<List<EtapaDto>>> GetByEvento(int idEvento)
    {
        var etapas = await _etapaService.GetByEventoAsync(idEvento);
        return Ok(etapas);
    }

    /// <summary>
    /// Busca etapa por ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EtapaDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EtapaDto>> GetById(int id)
    {
        var etapa = await _etapaService.GetByIdAsync(id);

        if (etapa == null)
            return NotFound($"Etapa com ID {id} não encontrada");

        return Ok(etapa);
    }

    /// <summary>
    /// Cria nova etapa
    /// </summary>
    /// <remarks>
    /// Para ENDURO: Configure NumeroEspeciais, NumeroVoltas, PrimeiraVoltaValida, PenalidadePorFaltaSegundos
    /// Para CIRCUITO: Configure DuracaoCorridaMinutos, VoltasAposTempoMinimo
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EtapaDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<EtapaDto>> Create([FromBody] EtapaCreateDto dto)
    {
        var etapa = await _etapaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = etapa.Id }, etapa);
    }

    /// <summary>
    /// Atualiza etapa
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EtapaDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EtapaDto>> Update(int id, [FromBody] EtapaUpdateDto dto)
    {
        var etapa = await _etapaService.UpdateAsync(id, dto);
        return Ok(etapa);
    }

    /// <summary>
    /// Altera status da etapa
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EtapaDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EtapaDto>> AlterarStatus(
        int id,
        [FromBody] AlterarStatusDto dto)
    {
        var etapa = await _etapaService.AlterarStatusAsync(id, dto.Status);
        return Ok(etapa);
    }

    /// <summary>
    /// Deleta etapa
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _etapaService.DeleteAsync(id);
        return NoContent();
    }
}
