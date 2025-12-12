using DevNationCrono.API.Middlewares;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Tags("Pilotos")]
//[Authorize]
public class PilotosController : ControllerBase
{
    private readonly IPilotoService _pilotoService;
    private readonly ILogger<PilotosController> _logger;

    public PilotosController(
        IPilotoService pilotoService,
        ILogger<PilotosController> logger)
    {
        _pilotoService = pilotoService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os pilotos
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [Authorize(Roles = "Admin,Organizador")]
    public async Task<ActionResult<List<PilotoDto>>> GetAll()
    {
        try
        {
            var pilotos = await _pilotoService.GetAllAsync();
            return Ok(pilotos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar pilotos");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Busca piloto por ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PilotoResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PilotoResponseDto>> GetById(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // Piloto só vê seus próprios dados
        if (userRole == "Piloto" && userId != id)
            return Forbid();

        var piloto = await _pilotoService.GetByIdAsync(id);

        if (piloto == null)
            return NotFound();

        return Ok(piloto);
    }

    /// <summary>
    /// Cadastra novo piloto no sistema
    /// </summary>
    /// <param name="dto">Dados do piloto</param>
    /// <returns>Piloto cadastrado com sucesso</returns>
    /// <response code="201">Piloto criado com sucesso</response>
    /// <response code="400">Dados inválidos ou CPF/Email já cadastrados</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost]
    [AllowAnonymous]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PilotoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PilotoResponseDto>> Cadastrar([FromBody] PilotoCadastroDto dto)
    {
        var piloto = await _pilotoService.CadastrarAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = piloto.Id }, piloto);
    }

    /// <summary>
    /// Atualiza dados do piloto
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion("1.0")]
    [Authorize]
    [ProducesResponseType(typeof(PilotoResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PilotoResponseDto>> Atualizar(
        int id,
        [FromBody] PilotoAtualizacaoDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // Piloto só atualiza seus próprios dados
        if (userRole == "Piloto" && userId != id)
            return Forbid();

        var piloto = await _pilotoService.AtualizarAsync(id, dto);
        return Ok(piloto);
    }

    /// <summary>
    /// Deleta piloto (soft delete)
    /// </summary>
    [MapToApiVersion("1.0")]
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deletar(int id)
    {
        await _pilotoService.DeletarAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Retorna dados do piloto logado
    /// </summary>
    [HttpGet("perfil")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PilotoResponseDto), 200)]
    public async Task<ActionResult<PilotoResponseDto>> GetPerfil()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var piloto = await _pilotoService.GetByIdAsync(userId);

        if (piloto == null)
            return NotFound();

        return Ok(piloto);
    }

    /// <summary>
    /// Lista pilotos com paginação e filtros
    /// </summary>
    [HttpGet("paginado")]
    [Authorize(Roles = "Admin,Organizador")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedResult<PilotoResponseDto>), 200)]
    public async Task<ActionResult<PagedResult<PilotoResponseDto>>> GetPaged(
        [FromQuery] PilotoFilterParams filterParams)
    {
        var result = await _pilotoService.GetPagedAsync(filterParams);
        return Ok(result);
    }

    /// <summary>
    /// Busca piloto por CPF
    /// </summary>
    [HttpGet("cpf/{cpf}")]
    [Authorize(Roles = "Admin,Organizador")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PilotoResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PilotoResponseDto>> GetByCpf(string cpf)
    {
        var piloto = await _pilotoService.GetByCpfAsync(cpf);

        if (piloto == null)
            return NotFound();

        return Ok(piloto);
    }
}
