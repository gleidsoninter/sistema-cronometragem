using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ColetoresController : ControllerBase
{
    private readonly IDispositivoColetorRepository _coletorRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ColetoresController> _logger;

    public ColetoresController(
        IDispositivoColetorRepository coletorRepository,
        ITokenService tokenService,
        ILogger<ColetoresController> logger)
    {
        _coletorRepository = coletorRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Registra novo dispositivo coletor (apenas Organizador/Admin)
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "Organizador,Admin")]
    public async Task<ActionResult<LoginResponseDto>> RegistrarColetor([FromBody] RegisterColetorDto dto)
    {
        try
        {
            // Verificar se DeviceId já está registrado
            if (await _coletorRepository.DeviceIdExistsAsync(dto.DeviceId))
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Dispositivo já registrado"
                });
            }

            // Criar coletor
            var coletor = new DispositivoColetor
            {
                IdEvento = dto.IdEvento,
                IdEtapa = dto.IdEtapa,
                Nome = dto.Nome,
                Tipo = dto.Tipo,
                IdEspecial = dto.IdEspecial,
                DeviceId = dto.DeviceId,
                Modelo = dto.Modelo,
                VersaoApp = dto.VersaoApp,
                StatusConexao = "OFFLINE",
                Ativo = true
            };

            // Gerar token JWT para o coletor
            var token = _tokenService.GerarTokenColetor(coletor);
            coletor.Token = token;

            await _coletorRepository.AddAsync(coletor);

            _logger.LogInformation("Novo coletor registrado: {DeviceId}", dto.DeviceId);

            return Ok(new
            {
                sucesso = true,
                mensagem = "Coletor registrado com sucesso",
                coletorId = coletor.Id,
                token = token,
                coletor = new
                {
                    coletor.Id,
                    coletor.Nome,
                    coletor.DeviceId,
                    coletor.Tipo,
                    coletor.IdEspecial
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar coletor");
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Login de coletor (usando DeviceId e token)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> LoginColetor([FromBody] ColetorLoginDto dto)
    {
        try
        {
            // Buscar coletor por DeviceId
            var coletor = await _coletorRepository.GetByDeviceIdAsync(dto.DeviceId);

            if (coletor == null)
            {
                return Unauthorized(new
                {
                    sucesso = false,
                    mensagem = "Dispositivo não registrado"
                });
            }

            if (!coletor.Ativo)
            {
                return Unauthorized(new
                {
                    sucesso = false,
                    mensagem = "Dispositivo desativado"
                });
            }

            // Verificar se o token bate
            if (coletor.Token != dto.Token)
            {
                _logger.LogWarning("Tentativa de login com token inválido: {DeviceId}", dto.DeviceId);

                return Unauthorized(new
                {
                    sucesso = false,
                    mensagem = "Token inválido"
                });
            }

            // Atualizar status e última conexão
            coletor.StatusConexao = "ONLINE";
            coletor.UltimaConexao = DateTime.UtcNow;
            await _coletorRepository.UpdateAsync(coletor);

            _logger.LogInformation("Coletor conectado: {DeviceId}", dto.DeviceId);

            return Ok(new
            {
                sucesso = true,
                mensagem = "Login realizado com sucesso",
                token = coletor.Token,
                coletor = new
                {
                    coletor.Id,
                    coletor.Nome,
                    coletor.DeviceId,
                    coletor.Tipo,
                    coletor.IdEvento,
                    coletor.IdEtapa,
                    coletor.IdEspecial
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login do coletor");
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Atualiza status de conexão do coletor (heartbeat)
    /// </summary>
    [HttpPost("heartbeat")]
    [Authorize(Roles = "Coletor")]
    public async Task<IActionResult> Heartbeat()
    {
        try
        {
            var deviceIdClaim = User.FindFirst("deviceId")?.Value;

            if (string.IsNullOrEmpty(deviceIdClaim))
            {
                return Unauthorized();
            }

            var coletor = await _coletorRepository.GetByDeviceIdAsync(deviceIdClaim);

            if (coletor == null)
            {
                return NotFound();
            }

            // Atualizar última conexão
            coletor.UltimaConexao = DateTime.UtcNow;
            coletor.StatusConexao = "ONLINE";
            await _coletorRepository.UpdateAsync(coletor);

            return Ok(new
            {
                sucesso = true,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no heartbeat");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Lista coletores de um evento (Organizador/Admin)
    /// </summary>
    [HttpGet("evento/{idEvento}")]
    [Authorize(Roles = "Organizador,Admin")]
    public async Task<ActionResult> ListarColetoresPorEvento(int idEvento)
    {
        try
        {
            var coletores = await _coletorRepository.GetByEventoAsync(idEvento);

            return Ok(coletores.Select(c => new
            {
                c.Id,
                c.Nome,
                c.DeviceId,
                c.Tipo,
                c.IdEspecial,
                c.Modelo,
                c.StatusConexao,
                c.UltimaConexao,
                c.LeiturasPendentes
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar coletores");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Desativa coletor (Organizador/Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organizador,Admin")]
    public async Task<IActionResult> DesativarColetor(int id)
    {
        try
        {
            var coletor = await _coletorRepository.GetByIdAsync(id);

            if (coletor == null)
            {
                return NotFound();
            }

            coletor.Ativo = false;
            coletor.StatusConexao = "INATIVO";
            await _coletorRepository.UpdateAsync(coletor);

            _logger.LogInformation("Coletor desativado: {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar coletor");
            return StatusCode(500);
        }
    }
}
