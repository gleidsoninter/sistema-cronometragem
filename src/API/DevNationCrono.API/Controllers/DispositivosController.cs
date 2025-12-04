using DevNationCrono.API.Hubs;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DevNationCrono.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DispositivosController : ControllerBase
{
    private readonly IDispositivoService _dispositivoService;
    private readonly IHubContext<CronometragemHub> _hubContext;
    private readonly ILogger<DispositivosController> _logger;

    public DispositivosController(
        IDispositivoService dispositivoService,
        IHubContext<CronometragemHub> hubContext,
        ILogger<DispositivosController> logger)
    {
        _dispositivoService = dispositivoService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os dispositivos coletores
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<ActionResult<List<DispositivoColetorDto>>> ListarDispositivos(
        [FromQuery] int? idEtapa = null)
    {
        var dispositivos = await _dispositivoService.ListarAsync(idEtapa);
        return Ok(dispositivos);
    }

    /// <summary>
    /// Obtém um dispositivo específico
    /// </summary>
    [HttpGet("{deviceId}")]
    [Authorize(Roles = "Admin,Operador,Coletor")]
    public async Task<ActionResult<DispositivoColetorDto>> ObterDispositivo(string deviceId)
    {
        var dispositivo = await _dispositivoService.ObterPorIdAsync(deviceId);

        if (dispositivo == null)
            return NotFound(new { mensagem = "Dispositivo não encontrado" });

        return Ok(dispositivo);
    }

    /// <summary>
    /// Registra um novo dispositivo coletor
    /// </summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistroDispositivoResultDto>> RegistrarDispositivo(
        [FromBody] RegistroDispositivoDto dto)
    {
        try
        {
            _logger.LogInformation("Registrando dispositivo: {DeviceId}", dto.DeviceId);

            var resultado = await _dispositivoService.RegistrarAsync(dto);

            if (resultado.Sucesso)
            {
                // Notificar via SignalR
                await _hubContext.Clients.Group("operadores").SendAsync(
                    "DispositivoRegistrado",
                    new
                    {
                        deviceId = dto.DeviceId,
                        nome = dto.Nome,
                        tipo = dto.Tipo,
                        timestamp = DateTime.UtcNow
                    });

                return Ok(resultado);
            }

            return BadRequest(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar dispositivo {DeviceId}", dto.DeviceId);
            return StatusCode(500, new { mensagem = "Erro interno ao registrar dispositivo" });
        }
    }

    /// <summary>
    /// Atualiza configuração de um dispositivo
    /// </summary>
    [HttpPut("{deviceId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<ActionResult<DispositivoColetorDto>> AtualizarDispositivo(
        string deviceId,
        [FromBody] AtualizarDispositivoDto dto)
    {
        var dispositivo = await _dispositivoService.AtualizarAsync(deviceId, dto);

        if (dispositivo == null)
            return NotFound(new { mensagem = "Dispositivo não encontrado" });

        return Ok(dispositivo);
    }

    /// <summary>
    /// Recebe heartbeat de um dispositivo
    /// </summary>
    [HttpPost("heartbeat")]
    [Authorize(Roles = "Coletor")]
    public async Task<ActionResult<HeartbeatResponseDto>> Heartbeat(
        [FromBody] HeartbeatDto dto)
    {
        try
        {
            var resultado = await _dispositivoService.ProcessarHeartbeatAsync(dto);

            // Notificar via SignalR para monitoramento em tempo real
            await _hubContext.Clients.Group("monitoramento").SendAsync(
                "HeartbeatRecebido",
                new
                {
                    deviceId = dto.DeviceId,
                    nome = dto.Nome,
                    tipo = dto.Tipo,
                    bateria = dto.NivelBateria,
                    pendentes = dto.LeiturasPendentes,
                    totalSessao = dto.TotalLeiturasSessao,
                    timestamp = DateTime.UtcNow
                });

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar heartbeat do dispositivo {DeviceId}", dto.DeviceId);
            return StatusCode(500, new { mensagem = "Erro ao processar heartbeat" });
        }
    }

    /// <summary>
    /// Obtém a hora do servidor para sincronização NTP
    /// </summary>
    [HttpGet("tempo")]
    [AllowAnonymous]
    public ActionResult<TempoServidorDto> ObterTempoServidor()
    {
        var agora = DateTime.UtcNow;

        return Ok(new TempoServidorDto
        {
            TimestampUtc = agora,
            TimestampUnixMs = new DateTimeOffset(agora).ToUnixTimeMilliseconds(),
            Timezone = "UTC"
        });
    }

    /// <summary>
    /// Lista dispositivos ativos (com heartbeat recente)
    /// </summary>
    [HttpGet("ativos")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<ActionResult<List<DispositivoStatusDto>>> ListarAtivos(
        [FromQuery] int? idEtapa = null,
        [FromQuery] int minutosTimeout = 5)
    {
        var dispositivos = await _dispositivoService.ListarAtivosAsync(idEtapa, minutosTimeout);
        return Ok(dispositivos);
    }

    /// <summary>
    /// Obtém estatísticas dos dispositivos
    /// </summary>
    [HttpGet("estatisticas")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<ActionResult<EstatisticasDispositivosDto>> ObterEstatisticas(
        [FromQuery] int? idEtapa = null)
    {
        var stats = await _dispositivoService.ObterEstatisticasAsync(idEtapa);
        return Ok(stats);
    }
}
