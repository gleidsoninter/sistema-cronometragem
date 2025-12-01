using DevNationCrono.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CronometragemController : ControllerBase
{
    private readonly ILogger<CronometragemController> _logger;

    public CronometragemController(ILogger<CronometragemController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Envia leitura do coletor - APENAS COLETORES!
    /// </summary>
    [HttpPost("leitura")]
    [Authorize(Roles = "Coletor")]
    public async Task<IActionResult> EnviarLeitura([FromBody] LeituraDto dto)
    {
        try
        {
            // Pegar info do coletor do token
            var deviceId = User.FindFirst("deviceId")?.Value;
            var coletorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(coletorId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Leitura recebida do coletor {DeviceId}: Moto {Numero}",
                deviceId, dto.NumeroMoto);

            // TODO: Processar leitura (faremos nas próximas aulas)

            return Ok(new
            {
                sucesso = true,
                mensagem = "Leitura registrada",
                coletorId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar leitura");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Visualiza resultados - PÚBLICO
    /// </summary>
    [HttpGet("resultados/{idEtapa}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResultados(int idEtapa)
    {
        // Resultados são públicos, qualquer um pode ver
        return Ok(new { mensagem = "Resultados virão aqui" });
    }

    /// <summary>
    /// Corrige leitura - Organizador/Admin
    /// </summary>
    [HttpPut("leitura/{id}")]
    [Authorize(Roles = "Organizador,Admin")]
    public async Task<IActionResult> CorrigirLeitura(int id, [FromBody] object correcao)
    {
        // Só organizadores podem corrigir leituras
        return Ok(new { mensagem = "Leitura corrigida" });
    }
}
