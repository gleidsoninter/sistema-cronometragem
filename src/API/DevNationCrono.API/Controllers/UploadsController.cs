using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

/// <summary>
/// Controller para upload de arquivos (cartazes, imagens, etc.)
/// </summary>
[ApiController]
[Route("api/v1.0/uploads")]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadsController> _logger;

    // Extensões permitidas para imagens
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    // Tamanho máximo: 5MB
    private const long MaxFileSize = 5 * 1024 * 1024;

    public UploadsController(IWebHostEnvironment environment, ILogger<UploadsController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Upload de cartaz de evento
    /// </summary>
    /// <param name="idEvento">ID do evento</param>
    /// <param name="file">Arquivo de imagem</param>
    /// <returns>URL da imagem salva</returns>
    [HttpPost("eventos/{idEvento}/cartaz")]
    [Authorize(Roles = "Admin,Organizador")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadCartazEvento(int idEvento, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "Arquivo muito grande. Máximo: 5MB" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return BadRequest(new { message = $"Extensão não permitida. Use: {string.Join(", ", _allowedExtensions)}" });

        try
        {
            // Criar diretório se não existir
            var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "eventos", "cartazes");
            Directory.CreateDirectory(uploadsPath);

            // Nome único para o arquivo
            var fileName = $"evento_{idEvento}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Remover cartaz anterior se existir
            var existingFiles = Directory.GetFiles(uploadsPath, $"evento_{idEvento}_*");
            foreach (var existingFile in existingFiles)
            {
                System.IO.File.Delete(existingFile);
            }

            // Salvar arquivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL relativa para acesso
            var url = $"/uploads/eventos/cartazes/{fileName}";

            _logger.LogInformation("Cartaz do evento {IdEvento} salvo: {FileName}", idEvento, fileName);

            return Ok(new
            {
                url = url,
                fileName = fileName,
                size = file.Length,
                message = "Cartaz enviado com sucesso!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer upload do cartaz do evento {IdEvento}", idEvento);
            return StatusCode(500, new { message = "Erro ao salvar arquivo" });
        }
    }

    /// <summary>
    /// Remover cartaz de evento
    /// </summary>
    [HttpDelete("eventos/{idEvento}/cartaz")]
    [Authorize(Roles = "Admin,Organizador")]
    public IActionResult RemoverCartazEvento(int idEvento)
    {
        try
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "eventos", "cartazes");
            var existingFiles = Directory.GetFiles(uploadsPath, $"evento_{idEvento}_*");

            foreach (var file in existingFiles)
            {
                System.IO.File.Delete(file);
            }

            return Ok(new { message = "Cartaz removido com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover cartaz do evento {IdEvento}", idEvento);
            return StatusCode(500, new { message = "Erro ao remover arquivo" });
        }
    }

    /// <summary>
    /// Upload de banner de campeonato
    /// </summary>
    [HttpPost("campeonatos/{idCampeonato}/banner")]
    [Authorize(Roles = "Admin,Organizador")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadBannerCampeonato(int idCampeonato, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "Arquivo muito grande. Máximo: 5MB" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return BadRequest(new { message = $"Extensão não permitida. Use: {string.Join(", ", _allowedExtensions)}" });

        try
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "campeonatos", "banners");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"campeonato_{idCampeonato}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Remover banner anterior
            var existingFiles = Directory.GetFiles(uploadsPath, $"campeonato_{idCampeonato}_*");
            foreach (var existingFile in existingFiles)
            {
                System.IO.File.Delete(existingFile);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/campeonatos/banners/{fileName}";

            return Ok(new
            {
                url = url,
                fileName = fileName,
                size = file.Length,
                message = "Banner enviado com sucesso!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer upload do banner do campeonato {IdCampeonato}", idCampeonato);
            return StatusCode(500, new { message = "Erro ao salvar arquivo" });
        }
    }
}
