using System.Security.Claims;
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
public class AuthController : ControllerBase
{
    private readonly IPilotoRepository _pilotoRepository;
    private readonly IDispositivoColetorRepository _dispositivoRepository;  // ✅ Adicionado
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPilotoRepository pilotoRepository,
        IDispositivoColetorRepository dispositivoRepository,  // ✅ Adicionado
        IUsuarioRepository usuarioRepository,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _pilotoRepository = pilotoRepository;
        _dispositivoRepository = dispositivoRepository;  // ✅ Adicionado
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Login de piloto
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            // Buscar piloto por email
            var piloto = await _pilotoRepository.GetByEmailAsync(request.Email);

            if (piloto == null)
            {
                return Ok(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha inválidos"
                });
            }

            // Verificar se está ativo
            if (!piloto.Ativo)
            {
                return Ok(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Conta desativada. Entre em contato com o suporte"
                });
            }

            // Verificar senha
            var senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, piloto.PasswordHash);

            if (!senhaValida)
            {
                _logger.LogWarning("Tentativa de login falhou para {Email}", request.Email);

                return Ok(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha inválidos"
                });
            }

            // Gerar token
            var token = _tokenService.GerarTokenPiloto(piloto);

            _logger.LogInformation("Login bem-sucedido para {Email}", request.Email);

            return Ok(new LoginResponseDto
            {
                Sucesso = true,
                Token = token,
                Usuario = new UsuarioDto
                {
                    Id = piloto.Id,
                    Nome = piloto.Nome,
                    Email = piloto.Email,
                    Role = "Piloto"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login");
            return StatusCode(500, new LoginResponseDto
            {
                Sucesso = false,
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Registra novo piloto
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] PilotoCadastroDto dto)
    {
        try
        {
            // Verificar se CPF já existe
            if (await _pilotoRepository.CpfExistsAsync(dto.Cpf))
            {
                return BadRequest(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "CPF já cadastrado"
                });
            }

            // Verificar se email já existe
            if (await _pilotoRepository.EmailExistsAsync(dto.Email))
            {
                return BadRequest(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Email já cadastrado"
                });
            }

            // Hash da senha
            var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Senha, salt);

            // Criar piloto
            var piloto = new Piloto
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Cpf = dto.Cpf,
                Telefone = dto.Telefone,
                DataNascimento = dto.DataNascimento,
                Cidade = dto.Cidade,
                Uf = dto.Uf,
                PasswordHash = hash,
                PasswordSalt = salt,
                Ativo = true
            };

            await _pilotoRepository.AddAsync(piloto);

            // Gerar token
            var token = _tokenService.GerarTokenPiloto(piloto);

            _logger.LogInformation("Novo piloto cadastrado: {Email}", dto.Email);

            return Ok(new LoginResponseDto
            {
                Sucesso = true,
                Token = token,
                Mensagem = "Cadastro realizado com sucesso!",
                Usuario = new UsuarioDto
                {
                    Id = piloto.Id,
                    Nome = piloto.Nome,
                    Email = piloto.Email,
                    Role = "Piloto"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no cadastro");
            return StatusCode(500, new LoginResponseDto
            {
                Sucesso = false,
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Verifica se token é válido
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Identity?.Name;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            valido = true,
            userId,
            userName,
            userRole
        });
    }

    /// <summary>
    /// Retorna dados do usuário logado
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var piloto = await _pilotoRepository.GetByIdAsync(userId);

        if (piloto == null)
        {
            return NotFound("Usuário não encontrado");
        }

        return Ok(new UsuarioDto
        {
            Id = piloto.Id,
            Nome = piloto.Nome,
            Email = piloto.Email,
            Role = "Piloto"
        });
    }

    /// <summary>
    /// Autentica um dispositivo coletor
    /// </summary>
    [HttpPost("dispositivo")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> AutenticarDispositivo([FromBody] AuthDispositivoDto dto)
    {
        try
        {
            // Buscar dispositivo
            var dispositivo = await _dispositivoRepository.GetByDeviceIdAsync(dto.DeviceId);

            if (dispositivo == null)
            {
                return Unauthorized(new AuthResultDto
                {
                    Sucesso = false,
                    Mensagem = "Dispositivo não encontrado ou inativo"
                });
            }

            //// Verificar se tem senha configurada
            //if (string.IsNullOrEmpty(dispositivo.SenhaHash))
            //{
            //    return Unauthorized(new AuthResultDto
            //    {
            //        Sucesso = false,
            //        Mensagem = "Dispositivo não configurado para autenticação"
            //    });
            //}

            //// Verificar senha
            //if (!BCrypt.Net.BCrypt.Verify(dto.Senha, dispositivo.SenhaHash))
            //{
            //    _logger.LogWarning("Tentativa de login falhou para dispositivo {DeviceId}", dto.DeviceId);

            //    return Unauthorized(new AuthResultDto
            //    {
            //        Sucesso = false,
            //        Mensagem = "Senha inválida"
            //    });
            //}

            // Gerar token JWT para o coletor
            var token = _tokenService.GerarTokenColetor(dispositivo);

            // Atualizar último acesso
            dispositivo.UltimaConexao = DateTime.UtcNow;
            dispositivo.StatusConexao = "ONLINE";
            await _dispositivoRepository.UpdateAsync(dispositivo);

            _logger.LogInformation("Dispositivo autenticado: {DeviceId}", dto.DeviceId);

            return Ok(new AuthResultDto
            {
                Sucesso = true,
                Token = token,
                ExpiraEm = DateTime.UtcNow.AddHours(24),
                DeviceId = dispositivo.DeviceId,
                Nome = dispositivo.Nome
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na autenticação do dispositivo");
            return StatusCode(500, new AuthResultDto
            {
                Sucesso = false,
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Login de administrador/organizador
    /// </summary>
    [HttpPost("login/admin")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> LoginAdmin([FromBody] LoginAdminRequestDto request)
    {
        try
        {
            // Buscar usuário por email
            var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);

            if (usuario == null)
            {
                return Ok(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha inválidos"
                });
            }

            // Verificar se está ativo
            if (!usuario.Ativo)
            {
                return Ok(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Conta desativada. Entre em contato com o suporte"
                });
            }

            // Verificar senha
            var senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, usuario.PasswordHash);

            if (!senhaValida)
            {
                _logger.LogWarning("Tentativa de login admin falhou para {Email}", request.Email);

                return Ok(new LoginResponseDto
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha inválidos"
                });
            }

            // Atualizar último acesso
            usuario.UltimoAcesso = DateTime.UtcNow;
            await _usuarioRepository.UpdateAsync(usuario);

            // Gerar token com a Role do usuário (Admin ou Organizador)
            var token = _tokenService.GerarTokenUsuario(usuario);

            _logger.LogInformation("Login admin bem-sucedido para {Email} com role {Role}",
                request.Email, usuario.Role);

            return Ok(new LoginResponseDto
            {
                Sucesso = true,
                Token = token,
                Usuario = new UsuarioDto
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Role = usuario.Role  // ← Admin ou Organizador
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login admin");
            return StatusCode(500, new LoginResponseDto
            {
                Sucesso = false,
                Mensagem = "Erro interno do servidor"
            });
        }
    }

}