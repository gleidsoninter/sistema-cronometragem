using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Services.Interfaces;

public interface INotificacaoTempoRealService
{
    // Passagens
    Task NotificarNovaPassagemAsync(Tempo tempo, Inscricao? inscricao, int posicaoAtual);
    Task NotificarPassagemEnduroAsync(Tempo tempo, Inscricao? inscricao, decimal? tempoEspecial);

    // Classificação
    Task NotificarClassificacaoAtualizadaAsync(int idEtapa);

    // Alertas
    Task NotificarMelhorVoltaGeralAsync(int idEtapa, int numeroMoto, string nomePiloto, string tempo);
    Task NotificarMelhorVoltaCategoriaAsync(int idEtapa, int idCategoria, int numeroMoto, string nomePiloto, string tempo);
    Task NotificarAbandonoAsync(int idEtapa, int numeroMoto, string nomePiloto);

    // Status da prova
    Task NotificarStatusProvaAsync(int idEtapa, string status);
    Task NotificarLargadaAsync(int idEtapa, DateTime horaLargada);
    Task NotificarBandeiraAsync(int idEtapa, DateTime horaBandeira);
    Task NotificarFimProvaAsync(int idEtapa);

    // Genérico
    Task EnviarParaGrupoAsync(string grupo, string metodo, object dados);
    Task EnviarParaTodosAsync(string metodo, object dados);
}
