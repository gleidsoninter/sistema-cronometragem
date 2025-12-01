using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InscricoesController : ControllerBase
{
    private readonly IInscricaoService _inscricaoService;
    private readonly ILogger<InscricoesController> _logger;

    public InscricoesController(
        IInscricaoService inscricaoService,
        ILogger<InscricoesController> logger)
    {
        _inscricaoService = inscricaoService;
        _logger = logger;
    }

    #region Consultas

    /// <summary>
    /// Lista inscrições com paginação e filtros
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(PagedResult<InscricaoResumoDto>), 200)]
    public async Task<ActionResult<PagedResult<InscricaoResumoDto>>> GetPaged(
        [FromQuery] InscricaoFilterParams filter)
    {
        var result = await _inscricaoService.GetPagedAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lista inscrições de um evento
    /// </summary>
    [HttpGet("evento/{idEvento}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<InscricaoDto>), 200)]
    public async Task<ActionResult<List<InscricaoDto>>> GetByEvento(int idEvento)
    {
        var inscricoes = await _inscricaoService.GetByEventoAsync(idEvento);
        return Ok(inscricoes);
    }

    /// <summary>
    /// Lista inscrições de uma etapa
    /// </summary>
    [HttpGet("etapa/{idEtapa}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<InscricaoDto>), 200)]
    public async Task<ActionResult<List<InscricaoDto>>> GetByEtapa(int idEtapa)
    {
        var inscricoes = await _inscricaoService.GetByEtapaAsync(idEtapa);
        return Ok(inscricoes);
    }

    /// <summary>
    /// Lista inscrições de uma categoria
    /// </summary>
    [HttpGet("categoria/{idCategoria}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<InscricaoDto>), 200)]
    public async Task<ActionResult<List<InscricaoDto>>> GetByCategoria(int idCategoria)
    {
        var inscricoes = await _inscricaoService.GetByCategoriaAsync(idCategoria);
        return Ok(inscricoes);
    }

    /// <summary>
    /// Lista inscrições do piloto logado
    /// </summary>
    [HttpGet("minhas")]
    [Authorize(Roles = "Piloto")]
    [ProducesResponseType(typeof(List<InscricaoDto>), 200)]
    public async Task<ActionResult<List<InscricaoDto>>> GetMinhasInscricoes()
    {
        var idPiloto = GetPilotoIdFromToken();
        var inscricoes = await _inscricaoService.GetByPilotoAsync(idPiloto);
        return Ok(inscricoes);
    }

    /// <summary>
    /// Lista inscrições de um piloto específico (admin)
    /// </summary>
    [HttpGet("piloto/{idPiloto}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(List<InscricaoDto>), 200)]
    public async Task<ActionResult<List<InscricaoDto>>> GetByPiloto(int idPiloto)
    {
        var inscricoes = await _inscricaoService.GetByPilotoAsync(idPiloto);
        return Ok(inscricoes);
    }

    /// <summary>
    /// Busca inscrição por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InscricaoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InscricaoDto>> GetById(int id)
    {
        var inscricao = await _inscricaoService.GetByIdAsync(id);

        if (inscricao == null)
            return NotFound("Inscrição não encontrada");

        return Ok(inscricao);
    }

    /// <summary>
    /// Estatísticas de inscrições de um evento
    /// </summary>
    [HttpGet("evento/{idEvento}/estatisticas")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(EstatisticasInscricaoDto), 200)]
    public async Task<ActionResult<EstatisticasInscricaoDto>> GetEstatisticas(int idEvento)
    {
        var estatisticas = await _inscricaoService.GetEstatisticasEventoAsync(idEvento);
        return Ok(estatisticas);
    }

    #endregion

    #region Inscrever

    /// <summary>
    /// Simula valores de inscrição (preview antes de confirmar)
    /// </summary>
    [HttpPost("simular")]
    [ProducesResponseType(typeof(SimulacaoInscricaoResponseDto), 200)]
    public async Task<ActionResult<SimulacaoInscricaoResponseDto>> SimularValores(
        [FromBody] SimularInscricaoDto dto)
    {
        var simulacao = await _inscricaoService.SimularValoresAsync(dto);
        return Ok(simulacao);
    }

    /// <summary>
    /// Cria inscrição em uma categoria
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InscricaoDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InscricaoDto>> Inscrever([FromBody] InscricaoCreateDto dto)
    {
        var inscricao = await _inscricaoService.InscreverAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = inscricao.Id }, inscricao);
    }

    /// <summary>
    /// Cria inscrição em múltiplas categorias (para Circuito)
    /// </summary>
    /// <remarks>
    /// Use este endpoint para inscrever o piloto em várias categorias de uma vez.
    /// O desconto é aplicado automaticamente a partir da segunda categoria.
    /// Para ENDURO, use o endpoint simples /inscricoes
    /// </remarks>
    [HttpPost("multiplas")]
    [ProducesResponseType(typeof(InscricaoMultiplaResponseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InscricaoMultiplaResponseDto>> InscreverMultiplas(
        [FromBody] InscricaoMultiplaCreateDto dto)
    {
        var resultado = await _inscricaoService.InscreverMultiplasCategoriasAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = resultado.Inscricoes.First().IdInscricao }, resultado);
    }

    /// <summary>
    /// Inscrição própria do piloto logado
    /// </summary>
    [HttpPost("auto-inscricao")]
    [Authorize(Roles = "Piloto")]
    [ProducesResponseType(typeof(InscricaoDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InscricaoDto>> AutoInscricao([FromBody] AutoInscricaoDto dto)
    {
        var idPiloto = GetPilotoIdFromToken();

        var inscricaoDto = new InscricaoCreateDto
        {
            IdPiloto = idPiloto,
            IdEvento = dto.IdEvento,
            IdCategoria = dto.IdCategoria,
            IdEtapa = dto.IdEtapa,
            NumeroMoto = dto.NumeroMoto,
            Observacoes = dto.Observacoes
        };

        var inscricao = await _inscricaoService.InscreverAsync(inscricaoDto);
        return CreatedAtAction(nameof(GetById), new { id = inscricao.Id }, inscricao);
    }

    #endregion

    #region Atualizar

    /// <summary>
    /// Atualiza inscrição
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(InscricaoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InscricaoDto>> Update(int id, [FromBody] InscricaoUpdateDto dto)
    {
        var inscricao = await _inscricaoService.UpdateAsync(id, dto);
        return Ok(inscricao);
    }

    /// <summary>
    /// Altera número de moto
    /// </summary>
    [HttpPatch("{id}/numero-moto")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(InscricaoDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InscricaoDto>> AlterarNumeroMoto(int id, [FromBody] AlterarNumeroMotoDto dto)
    {
        var inscricao = await _inscricaoService.AlterarNumeroMotoAsync(id, dto.NumeroMoto);
        return Ok(inscricao);
    }

    /// <summary>
    /// Valida se número de moto está disponível
    /// </summary>
    [HttpGet("validar-numero/{numeroMoto}/evento/{idEvento}")]
    [ProducesResponseType(typeof(bool), 200)]
    public async Task<ActionResult<object>> ValidarNumeroMoto(int numeroMoto, int idEvento)
    {
        var disponivel = await _inscricaoService.ValidarNumeroMotoAsync(numeroMoto, idEvento);
        return Ok(new { disponivel, numeroMoto, idEvento });
    }

    #endregion

    #region Pagamento

    /// <summary>
    /// Confirma pagamento de inscrição
    /// </summary>
    [HttpPost("{id}/confirmar-pagamento")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(InscricaoDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InscricaoDto>> ConfirmarPagamento(
        int id,
        [FromBody] ConfirmarPagamentoDto dto)
    {
        var inscricao = await _inscricaoService.ConfirmarPagamentoAsync(id, dto);
        return Ok(inscricao);
    }

    /// <summary>
    /// Gera QR Code PIX para pagamento
    /// </summary>
    [HttpPost("{id}/gerar-pix")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult> GerarPix(int id)
    {
        var qrCode = await _inscricaoService.GerarQrCodePixAsync(id);
        return Ok(new { qrCode, idInscricao = id });
    }

    /// <summary>
    /// Cancela inscrição
    /// </summary>
    [HttpPost("{id}/cancelar")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(InscricaoDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<InscricaoDto>> Cancelar(int id, [FromBody] CancelarInscricaoDto? dto = null)
    {
        var inscricao = await _inscricaoService.CancelarInscricaoAsync(id, dto?.Motivo);
        return Ok(inscricao);
    }

    #endregion

    private int GetPilotoIdFromToken()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                   ?? User.FindFirst("Id");

        if (idClaim == null || !int.TryParse(idClaim.Value, out int idPiloto))
        {
            throw new UnauthorizedAccessException("ID do piloto não encontrado no token");
        }

        return idPiloto;
    }
}

// DTOs auxiliares
public class AutoInscricaoDto
{
    [Required]
    public int IdEvento { get; set; }

    [Required]
    public int IdCategoria { get; set; }

    [Required]
    public int IdEtapa { get; set; }

    public int? NumeroMoto { get; set; }
    public string? Observacoes { get; set; }
}

public class AlterarNumeroMotoDto
{
    [Required]
    [Range(1, 9999)]
    public int NumeroMoto { get; set; }
}

public class CancelarInscricaoDto
{
    public string? Motivo { get; set; }
}
