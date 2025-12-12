using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface ICampeonatoService
{
    // CRUD Campeonato
    Task<List<CampeonatoResumoDto>> GetAllAsync(int? ano = null, int? idModalidade = null);
    Task<CampeonatoDto?> GetByIdAsync(int id);
    Task<CampeonatoDto> CreateAsync(CampeonatoCreateDto dto);
    Task<CampeonatoDto> UpdateAsync(int id, CampeonatoUpdateDto dto);
    Task<CampeonatoDto> AlterarStatusAsync(int id, string status);
    Task DeleteAsync(int id);

    // Pontuação
    Task<List<CampeonatoPontuacaoDto>> GetPontuacoesAsync(int idCampeonato);
    Task<List<CampeonatoPontuacaoDto>> SetPontuacoesAsync(int idCampeonato, List<CampeonatoPontuacaoCreateDto> pontuacoes);
    Task<List<CampeonatoPontuacaoDto>> ApplyPontuacaoTemplateAsync(int idCampeonato, string template);

    // Classificação
    Task<ClassificacaoCampeonatoDto> GetClassificacaoAsync(int idCampeonato);
    Task<ClassificacaoCategoriaCampeonatoDto> GetClassificacaoCategoriaAsync(int idCampeonato, int idCategoria);

    // Eventos
    Task<List<EventoResumoDto>> GetEventosAsync(int idCampeonato);
    Task VincularEventoAsync(int idCampeonato, int idEvento);
    Task DesvincularEventoAsync(int idCampeonato, int idEvento);
}