using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface ICategoriaService
{
    /// <summary>
    /// Obtém uma categoria pelo ID, incluindo contagem de inscritos e vagas disponíveis.
    /// </summary>
    Task<CategoriaDto?> GetByIdAsync(int id);

    /// <summary>
    /// Lista todas as categorias de um evento específico (incluindo as inativas).
    /// </summary>
    Task<List<CategoriaDto>> GetByModalidadeAsync(int idModalidade);

    /// <summary>
    /// Lista apenas categorias ativas de um evento, retornando um DTO resumido (ideal para combobox/selects no front).
    /// </summary>
    Task<List<CategoriaResumoDto>> GetActivesByModalidadeAsync(int idModalidade);

    /// <summary>
    /// Cria uma nova categoria com validações de regras de negócio (idade, cilindrada, unicidade de nome).
    /// </summary>
    Task<CategoriaDto> CreateAsync(CategoriaCreateDto dto);

    /// <summary>
    /// Atualiza dados da categoria. Bloqueia alteração se o evento já estiver finalizado.
    /// </summary>
    Task<CategoriaDto> UpdateAsync(int id, CategoriaUpdateDto dto);

    /// <summary>
    /// Remove uma categoria (Soft Delete ou Hard Delete dependendo do repo), desde que não tenha inscritos.
    /// </summary>
    Task DeleteAsync(int id);
}
