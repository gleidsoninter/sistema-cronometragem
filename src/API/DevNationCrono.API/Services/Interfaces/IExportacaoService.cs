namespace DevNationCrono.API.Services.Interfaces;

public interface IExportacaoService
{
    Task<byte[]> ExportarClassificacaoGeralPdfAsync(int idEtapa);
    Task<byte[]> ExportarClassificacaoCategoriaPdfAsync(int idEtapa, int idCategoria);
    Task<byte[]> ExportarResultadoPilotoPdfAsync(int idEtapa, int numeroMoto);
    Task<byte[]> ExportarRankingMelhorVoltaPdfAsync(int idEtapa);
}
