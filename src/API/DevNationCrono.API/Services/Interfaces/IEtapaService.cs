using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface IEtapaService
{
    /// <summary>
    /// Obtém os detalhes de uma etapa, incluindo contagem de leituras e coletores.
    /// </summary>
    Task<EtapaDto?> GetByIdAsync(int id);

    /// <summary>
    /// Lista todas as etapas vinculadas a um evento específico.
    /// </summary>
    Task<List<EtapaDto>> GetByEventoAsync(int idEvento);

    /// <summary>
    /// Cria uma nova etapa aplicando validações de data (dentro do evento) e regras específicas da modalidade (Enduro/Circuito).
    /// </summary>
    Task<EtapaDto> CreateAsync(EtapaCreateDto dto);

    /// <summary>
    /// Atualiza dados da etapa. Bloqueia alterações se a etapa estiver Finalizada ou Cancelada.
    /// </summary>
    Task<EtapaDto> UpdateAsync(int id, EtapaUpdateDto dto);

    /// <summary>
    /// Realiza a transição de status (Máquina de Estados) validando se a troca é permitida (ex: Agendada -> Em Andamento).
    /// </summary>
    Task<EtapaDto> AlterarStatusAsync(int id, string novoStatus);

    /// <summary>
    /// Remove uma etapa, desde que não existam leituras/tempos registrados nela.
    /// </summary>
    Task DeleteAsync(int id);
}
