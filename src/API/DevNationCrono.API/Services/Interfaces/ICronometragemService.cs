using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Services.Interfaces;

public interface ICronometragemService
{
    // Processar leituras
    Task<LeituraResponseDto> ProcessarLeituraAsync(LeituraDto leitura);
    Task<LoteLeituraResponseDto> ProcessarLoteLeituraAsync(LoteLeituraDto lote);

    // Consultar leituras
    Task<List<LeituraResponseDto>> GetLeiturasEtapaAsync(int idEtapa);
    Task<PagedResult<LeituraResponseDto>> GetLeiturasPagedAsync(LeituraFilterParams filter);
    Task<List<TempoCalculadoDto>> GetTemposPilotoAsync(int idEtapa, int numeroMoto);

    // Correção manual
    Task<LeituraResponseDto> CorrigirLeituraAsync(long id, CorrecaoTempoDto correcao);
    Task<LeituraResponseDto> DescartarLeituraAsync(long id, string motivo);
    Task<LeituraResponseDto> RestaurarLeituraAsync(long id);

    // Cálculos
    Task<decimal?> CalcularTempoEspecialAsync(int idEtapa, int numeroMoto, int idEspecial, int volta);
    Task RecalcularTemposEtapaAsync(int idEtapa);
    Task RecalcularMelhoresVoltasAsync(int idEtapa);

    // Dispositivos
    Task<ColetorLoginResponseDto> AutenticarColetorAsync(ColetorLoginDto dto);
    Task AtualizarHeartbeatAsync(ColetorHeartbeatDto dto);
    Task<List<DispositivoColetorDto>> GetDispositivosEtapaAsync(int idEtapa);
}
