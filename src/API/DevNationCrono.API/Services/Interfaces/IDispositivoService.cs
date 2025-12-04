using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface IDispositivoService
{
    Task<List<DispositivoColetorDto>> ListarAsync(int? idEtapa = null);
    Task<DispositivoColetorDto?> ObterPorIdAsync(string deviceId);
    Task<RegistroDispositivoResultDto> RegistrarAsync(RegistroDispositivoDto dto);
    Task<DispositivoColetorDto?> AtualizarAsync(string deviceId, AtualizarDispositivoDto dto);
    Task<HeartbeatResponseDto> ProcessarHeartbeatAsync(HeartbeatDto dto);
    Task<List<DispositivoStatusDto>> ListarAtivosAsync(int? idEtapa, int minutosTimeout);
    Task<EstatisticasDispositivosDto> ObterEstatisticasAsync(int? idEtapa);
}