using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PagamentosController : ControllerBase
{
    private readonly IPagamentoService _pagamentoService;
    private readonly ILogger<PagamentosController> _logger;

    public PagamentosController(
        IPagamentoService pagamentoService,
        ILogger<PagamentosController> logger)
    {
        _pagamentoService = pagamentoService;
        _logger = logger;
    }

    /// <summary>
    /// Gateway de pagamento ativo
    /// </summary>
    [HttpGet("gateway")]
    [AllowAnonymous]
    public ActionResult<object> GetGateway()
    {
        return Ok(new { gateway = _pagamentoService.GatewayAtivo });
    }

    /// <summary>
    /// Cria cobrança PIX para uma inscrição
    /// </summary>
    [HttpPost("pix/inscricao/{idInscricao}")]
    [ProducesResponseType(typeof(CobrancaPixResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CobrancaPixResponseDto>> CriarCobrancaPix(int idInscricao)
    {
        var cobranca = await _pagamentoService.CriarCobrancaPixAsync(idInscricao);
        return Ok(cobranca);
    }

    /// <summary>
    /// Cria cobrança PIX para múltiplas inscrições
    /// </summary>
    [HttpPost("pix/multiplas")]
    [ProducesResponseType(typeof(CobrancaPixResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CobrancaPixResponseDto>> CriarCobrancaPixMultiplas(
        [FromBody] CriarCobrancaPixDto dto)
    {
        if (dto.IdsInscricoes != null && dto.IdsInscricoes.Any())
        {
            var cobranca = await _pagamentoService.CriarCobrancaPixMultiplasAsync(dto.IdsInscricoes);
            return Ok(cobranca);
        }
        else
        {
            var cobranca = await _pagamentoService.CriarCobrancaPixAsync(dto.IdInscricao);
            return Ok(cobranca);
        }
    }

    /// <summary>
    /// Consulta status de um pagamento
    /// </summary>
    [HttpGet("{idExterno}")]
    [ProducesResponseType(typeof(ConsultaPagamentoResponseDto), 200)]
    public async Task<ActionResult<ConsultaPagamentoResponseDto>> ConsultarPagamento(string idExterno)
    {
        var pagamento = await _pagamentoService.ConsultarPagamentoAsync(idExterno);
        return Ok(pagamento);
    }

    /// <summary>
    /// Consulta status do pagamento de uma inscrição
    /// </summary>
    [HttpGet("inscricao/{idInscricao}")]
    [ProducesResponseType(typeof(ConsultaPagamentoResponseDto), 200)]
    public async Task<ActionResult<ConsultaPagamentoResponseDto>> ConsultarPagamentoPorInscricao(int idInscricao)
    {
        var pagamento = await _pagamentoService.ConsultarPagamentoPorInscricaoAsync(idInscricao);
        return Ok(pagamento);
    }

    /// <summary>
    /// Cancela uma cobrança pendente
    /// </summary>
    [HttpDelete("{idExterno}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> CancelarCobranca(string idExterno)
    {
        var resultado = await _pagamentoService.CancelarCobrancaAsync(idExterno);

        if (resultado)
            return Ok(new { mensagem = "Cobrança cancelada com sucesso" });
        else
            return BadRequest(new { mensagem = "Não foi possível cancelar a cobrança" });
    }

    /// <summary>
    /// Verifica e atualiza cobranças expiradas (para job agendado)
    /// </summary>
    [HttpPost("verificar-expiradas")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> VerificarExpiradas()
    {
        var count = await _pagamentoService.VerificarCobrancasExpiradasAsync();
        return Ok(new { mensagem = $"{count} cobrança(s) atualizada(s)" });
    }
}
