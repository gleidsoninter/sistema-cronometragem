using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ModalidadesController : ControllerBase
{
    private readonly IModalidadeService _modalidadeService;
    private readonly ILogger<ModalidadesController> _logger;

    public ModalidadesController(
        IModalidadeService modalidadeService,
        ILogger<ModalidadesController> logger)
    {
        _modalidadeService = modalidadeService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as modalidades
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ModalidadeDto>), 200)]
    public async Task<ActionResult<List<ModalidadeDto>>> GetAll()
    {
        var modalidades = await _modalidadeService.GetAllAsync();
        return Ok(modalidades);
    }

    /// <summary>
    /// Lista modalidades ativas (para dropdowns)
    /// </summary>
    [HttpGet("ativas")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ModalidadeResumoDto>), 200)]
    public async Task<ActionResult<List<ModalidadeResumoDto>>> GetActives()
    {
        var modalidades = await _modalidadeService.GetActivesAsync();
        return Ok(modalidades);
    }

    /// <summary>
    /// Lista modalidades por tipo de cronometragem
    /// </summary>
    [HttpGet("tipo/{tipo}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ModalidadeResumoDto>), 200)]
    public async Task<ActionResult<List<ModalidadeResumoDto>>> GetByTipo(string tipo)
    {
        var modalidades = await _modalidadeService.GetByTipoAsync(tipo.ToUpper());
        return Ok(modalidades);
    }

    /// <summary>
    /// Busca modalidade por ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ModalidadeDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ModalidadeDto>> GetById(int id)
    {
        var modalidade = await _modalidadeService.GetByIdAsync(id);

        if (modalidade == null)
            return NotFound($"Modalidade com ID {id} não encontrada");

        return Ok(modalidade);
    }

    /// <summary>
    /// Cria nova modalidade
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ModalidadeDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ModalidadeDto>> Create([FromBody] ModalidadeCreateDto dto)
    {
        var modalidade = await _modalidadeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = modalidade.Id }, modalidade);
    }

    /// <summary>
    /// Atualiza modalidade
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ModalidadeDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ModalidadeDto>> Update(int id, [FromBody] ModalidadeUpdateDto dto)
    {
        var modalidade = await _modalidadeService.UpdateAsync(id, dto);
        return Ok(modalidade);
    }

    /// <summary>
    /// Deleta modalidade (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _modalidadeService.DeleteAsync(id);
        return NoContent();
    }
}
