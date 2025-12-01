using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Repositories.Interfaces;

public interface IEtapaRepository
{
    /// <summary>
    /// Busca uma etapa pelo ID, trazendo os dados do Evento e Modalidade vinculados.
    /// </summary>
    Task<Etapa?> GetByIdAsync(int id);

    /// <summary>
    /// Lista todas as etapas de um evento, ordenadas pelo número da etapa.
    /// </summary>
    Task<List<Etapa>> GetByEventoAsync(int idEvento);

    /// <summary>
    /// Busca uma etapa específica pelo número dentro do evento (ex: Etapa 1 do Evento X).
    /// </summary>
    Task<Etapa?> GetByEventoNumeroAsync(int idEvento, int numeroEtapa);

    /// <summary>
    /// Persiste uma nova etapa no banco.
    /// </summary>
    Task<Etapa> AddAsync(Etapa etapa);

    /// <summary>
    /// Atualiza os dados de uma etapa existente.
    /// </summary>
    Task UpdateAsync(Etapa etapa);

    /// <summary>
    /// Realiza a exclusão lógica (Soft Delete) da etapa, mudando status para CANCELADA.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Verifica se uma etapa existe pelo ID (método leve, sem tracking).
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Valida se já existe uma etapa com este número no mesmo evento.
    /// Útil para evitar duplicidade na criação ou edição.
    /// </summary>
    /// <param name="excludeId">ID da etapa atual para ignorar na verificação (durante edição).</param>
    Task<bool> NumeroExistsNoEventoAsync(int numeroEtapa, int idEvento, int? excludeId = null);

    /// <summary>
    /// Conta quantos tempos/leituras foram registrados nesta etapa.
    /// Usado para impedir alterações críticas se a etapa já estiver em andamento.
    /// </summary>
    Task<int> CountLeiturasAsync(int id);

    /// <summary>
    /// Conta quantos dispositivos coletores estão ativos e vinculados a esta etapa.
    /// </summary>
    Task<int> CountColetoresAsync(int id);

    /// <summary>
    /// Sugere o próximo número de etapa disponível para um evento (MAX + 1).
    /// </summary>
    Task<int> GetProximoNumeroEtapaAsync(int idEvento);
}
