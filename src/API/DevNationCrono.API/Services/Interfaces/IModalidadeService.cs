using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface IModalidadeService
{
    Task<ModalidadeDto?> GetByIdAsync(int id);
    Task<List<ModalidadeDto>> GetAllAsync();
    Task<List<ModalidadeResumoDto>> GetActivesAsync();
    Task<List<ModalidadeResumoDto>> GetByTipoAsync(string tipoCronometragem);
    Task<ModalidadeDto> CreateAsync(ModalidadeCreateDto dto);
    Task<ModalidadeDto> UpdateAsync(int id, ModalidadeUpdateDto dto);
    Task DeleteAsync(int id);
}
