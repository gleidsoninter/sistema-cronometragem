using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ColetoresController : ControllerBase
{
    private readonly ICronometragemService _cronometragemService;
    private readonly IDispositivoColetorRepository _dispositivoRepository;
    private readonly ILogger<ColetoresController> _logger;

    public ColetoresController(
        ICronometragemService cronometragemService,
        IDispositivoColetorRepository dispositivoRepository,
        ILogger<ColetoresController> logger)
    {
        _cronometragemService = cronometragemService;
        _dispositivoRepository = dispositivoRepository;
        _logger = logger;
    }

    /// <summary>
    /// Autentica um coletor para uma etapa
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ColetorLoginResponseDto), 200)]
    public async Task<ActionResult<ColetorLoginResponseDto>> Login([FromBody] ColetorLoginDto dto)
    {
        var resultado = await _cronometragemService.AutenticarColetorAsync(dto);

        if (!resultado.Sucesso)
        {
            return Unauthorized(resultado);
        }

        return Ok(resultado);
    }

    /// <summary>
    /// Atualiza heartbeat do coletor (mantém conexão ativa)
    /// </summary>
    [HttpPost("heartbeat")]
    [Authorize(Roles = "Coletor")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Heartbeat([FromBody] ColetorHeartbeatDto dto)
    {
        await _cronometragemService.AtualizarHeartbeatAsync(dto);
        return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Lista coletores de uma etapa
    /// </summary>
    [HttpGet("etapa/{idEtapa}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(List<DispositivoColetorDto>), 200)]
    public async Task<ActionResult<List<DispositivoColetorDto>>> GetByEtapa(int idEtapa)
    {
        var dispositivos = await _cronometragemService.GetDispositivosEtapaAsync(idEtapa);
        return Ok(dispositivos);
    }

    /// <summary>
    /// Cadastra novo coletor
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(DispositivoColetorDto), 201)]
    public async Task<ActionResult<DispositivoColetorDto>> Cadastrar(
        [FromBody] DispositivoColetorCreateDto dto)
    {
        // Verificar se DeviceId já existe
        if (await _dispositivoRepository.ExisteDeviceIdAsync(dto.DeviceId))
        {
            return BadRequest(new { mensagem = "DeviceId já cadastrado" });
        }

        var dispositivo = new Models.Entities.DispositivoColetor
        {
            IdEvento = dto.IdEvento,
            IdEtapa = dto.IdEtapa,
            Nome = dto.Nome,
            Tipo = dto.Tipo,
            IdEspecial = dto.IdEspecial,
            DeviceId = dto.DeviceId,
            StatusConexao = "OFFLINE",
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        await _dispositivoRepository.AddAsync(dispositivo);

        // Recarregar com includes
        dispositivo = await _dispositivoRepository.GetByIdAsync(dispositivo.Id);

        var response = new DispositivoColetorDto
        {
            Id = dispositivo.Id,
            IdEvento = dispositivo.IdEvento,
            NomeEvento = dispositivo.Evento?.Nome ?? "",
            IdEtapa = dispositivo.IdEtapa,
            NomeEtapa = dispositivo.Etapa?.Nome ?? "",
            Nome = dispositivo.Nome,
            Tipo = dispositivo.Tipo,
            IdEspecial = dispositivo.IdEspecial,
            DeviceId = dispositivo.DeviceId,
            StatusConexao = dispositivo.StatusConexao,
            Ativo = dispositivo.Ativo
        };

        return CreatedAtAction(nameof(GetByEtapa), new { idEtapa = dto.IdEtapa }, response);
    }

    /// <summary>
    /// Atualiza coletor
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] DispositivoColetorUpdateDto dto)
    {
        var dispositivo = await _dispositivoRepository.GetByIdAsync(id);
        if (dispositivo == null)
            return NotFound();

        if (!string.IsNullOrEmpty(dto.Nome))
            dispositivo.Nome = dto.Nome;

        if (dto.IdEspecial.HasValue)
            dispositivo.IdEspecial = dto.IdEspecial;

        if (dto.Ativo.HasValue)
            dispositivo.Ativo = dto.Ativo.Value;

        await _dispositivoRepository.UpdateAsync(dispositivo);

        return Ok(new { mensagem = "Coletor atualizado" });
    }
}