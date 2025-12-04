using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exportar")]
public class ExportacaoController : ControllerBase
{
    private readonly IExportacaoService _exportacaoService;

    public ExportacaoController(IExportacaoService exportacaoService)
    {
        _exportacaoService = exportacaoService;
    }

    /// <summary>
    /// Exporta classificação geral para PDF
    /// </summary>
    [HttpGet("etapa/{idEtapa}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportarClassificacaoGeral(int idEtapa)
    {
        var pdf = await _exportacaoService.ExportarClassificacaoGeralPdfAsync(idEtapa);
        return File(pdf, "application/pdf", $"classificacao_etapa_{idEtapa}.pdf");
    }

    /// <summary>
    /// Exporta classificação de uma categoria para PDF
    /// </summary>
    [HttpGet("etapa/{idEtapa}/categoria/{idCategoria}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportarClassificacaoCategoria(int idEtapa, int idCategoria)
    {
        var pdf = await _exportacaoService.ExportarClassificacaoCategoriaPdfAsync(idEtapa, idCategoria);
        return File(pdf, "application/pdf", $"classificacao_categoria_{idCategoria}.pdf");
    }

    /// <summary>
    /// Exporta resultado de um piloto para PDF
    /// </summary>
    [HttpGet("etapa/{idEtapa}/moto/{numeroMoto}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportarResultadoPiloto(int idEtapa, int numeroMoto)
    {
        var pdf = await _exportacaoService.ExportarResultadoPilotoPdfAsync(idEtapa, numeroMoto);
        return File(pdf, "application/pdf", $"resultado_moto_{numeroMoto}.pdf");
    }

    /// <summary>
    /// Exporta ranking de melhor volta para PDF
    /// </summary>
    [HttpGet("etapa/{idEtapa}/melhor-volta/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportarRankingMelhorVolta(int idEtapa)
    {
        var pdf = await _exportacaoService.ExportarRankingMelhorVoltaPdfAsync(idEtapa);
        return File(pdf, "application/pdf", $"ranking_melhor_volta_{idEtapa}.pdf");
    }
}
