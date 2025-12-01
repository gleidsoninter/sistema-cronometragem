using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Services.Interfaces;

public interface IPilotoService
{
    Task<PilotoResponseDto?> GetByIdAsync(int id);
    Task<List<PilotoResponseDto>> GetAllAsync();
    Task<PilotoResponseDto> CadastrarAsync(PilotoCadastroDto dto);
    Task<PilotoResponseDto> AtualizarAsync(int id, PilotoAtualizacaoDto dto);
    Task DeletarAsync(int id);

    Task<PilotoResponseDto?> GetByCpfAsync(string cpf);

    Task<PagedResult<PilotoResponseDto>> GetPagedAsync(PilotoFilterParams filterParams);
    //Task<PilotoResponseDto?> LoginAsync(string email, string senha);
}
