using DevNationCrono.API.Hubs;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace DevNationCrono.API.Services.Implementations;

public class NotificacaoTempoRealService : INotificacaoTempoRealService
{
    private readonly IHubContext<CronometragemHub> _hubContext;
    private readonly IResultadoCircuitoService _resultadoCircuitoService;
    private readonly ILogger<NotificacaoTempoRealService> _logger;

    public NotificacaoTempoRealService(
        IHubContext<CronometragemHub> hubContext,
        IResultadoCircuitoService resultadoCircuitoService,
        ILogger<NotificacaoTempoRealService> logger)
    {
        _hubContext = hubContext;
        _resultadoCircuitoService = resultadoCircuitoService;
        _logger = logger;
    }

    #region Passagens

    public async Task NotificarNovaPassagemAsync(
        Tempo tempo,
        Inscricao? inscricao,
        int posicaoAtual)
    {
        var notificacao = new PassagemNotificacaoDto
        {
            Tipo = "PASSAGEM",
            Timestamp = tempo.Timestamp,
            IdEtapa = tempo.IdEtapa,
            NumeroMoto = tempo.NumeroMoto,
            NomePiloto = inscricao?.Piloto?.Nome ?? "N/A",
            Categoria = inscricao?.Categoria?.Nome ?? "N/A",
            Volta = tempo.Volta,
            TempoVolta = tempo.TempoFormatado ?? "-",
            PosicaoAtual = posicaoAtual,
            MelhorVoltaPessoal = tempo.MelhorVolta,
            MelhorVoltaGeral = false, // Será verificado separadamente
            MelhorVoltaCategoria = false
        };

        // Enviar para grupo da etapa
        var grupoEtapa = $"Etapa_{tempo.IdEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("NovaPassagem", notificacao);

        // Enviar para grupo da categoria
        if (inscricao != null)
        {
            var grupoCategoria = $"Etapa_{tempo.IdEtapa}_Categoria_{inscricao.IdCategoria}";
            await _hubContext.Clients.Group(grupoCategoria)
                .SendAsync("NovaPassagem", notificacao);
        }

        // Enviar para grupo do piloto
        var grupoPiloto = $"Etapa_{tempo.IdEtapa}_Moto_{tempo.NumeroMoto}";
        await _hubContext.Clients.Group(grupoPiloto)
            .SendAsync("MinhaPassagem", notificacao);

        _logger.LogDebug(
            "Notificação de passagem enviada: Etapa {Etapa}, Moto {Moto}, Volta {Volta}",
            tempo.IdEtapa, tempo.NumeroMoto, tempo.Volta);
    }

    public async Task NotificarPassagemEnduroAsync(
        Tempo tempo,
        Inscricao? inscricao,
        decimal? tempoEspecial)
    {
        var tipoLeitura = tempo.Tipo == "E" ? "ENTRADA" : "SAIDA";

        var notificacao = new
        {
            tipo = $"ENDURO_{tipoLeitura}",
            timestamp = tempo.Timestamp,
            idEtapa = tempo.IdEtapa,
            numeroMoto = tempo.NumeroMoto,
            nomePiloto = inscricao?.Piloto?.Nome ?? "N/A",
            categoria = inscricao?.Categoria?.Nome ?? "N/A",
            especial = tempo.IdEspecial,
            volta = tempo.Volta,
            tipoLeitura,
            tempoEspecial = tempoEspecial.HasValue ? FormatarTempo(tempoEspecial.Value) : null
        };

        var grupoEtapa = $"Etapa_{tempo.IdEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("LeituraEnduro", notificacao);

        var grupoPiloto = $"Etapa_{tempo.IdEtapa}_Moto_{tempo.NumeroMoto}";
        await _hubContext.Clients.Group(grupoPiloto)
            .SendAsync("MinhaLeituraEnduro", notificacao);
    }

    #endregion

    #region Classificação

    public async Task NotificarClassificacaoAtualizadaAsync(int idEtapa)
    {
        try
        {
            // Buscar classificação atualizada
            var resumo = await _resultadoCircuitoService.GetResumoTempoRealAsync(idEtapa);

            var atualizacao = new ClassificacaoAtualizacaoDto
            {
                Tipo = "CLASSIFICACAO",
                Timestamp = DateTime.UtcNow,
                IdEtapa = idEtapa,
                TipoCronometragem = "CIRCUITO",
                TotalPilotos = resumo.Count,
                VoltasLider = resumo.FirstOrDefault()?.Voltas ?? 0,
                Top10 = resumo.Take(10).Select(r => new PosicaoResumoDto
                {
                    Posicao = r.PosicaoGeral,
                    NumeroMoto = r.NumeroMoto,
                    NomePiloto = r.NomePiloto,
                    Categoria = r.Categoria,
                    Voltas = r.Voltas,
                    Tempo = r.TempoTotal,
                    Diferenca = r.Diferenca,
                    UltimaVolta = r.UltimaVolta,
                    Status = r.Status
                }).ToList()
            };

            var grupoEtapa = $"Etapa_{idEtapa}";
            await _hubContext.Clients.Group(grupoEtapa)
                .SendAsync("ClassificacaoAtualizada", atualizacao);

            _logger.LogDebug(
                "Classificação atualizada enviada: Etapa {Etapa}, {Total} pilotos",
                idEtapa, resumo.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar classificação: Etapa {Etapa}", idEtapa);
        }
    }

    #endregion

    #region Alertas

    public async Task NotificarMelhorVoltaGeralAsync(
        int idEtapa,
        int numeroMoto,
        string nomePiloto,
        string tempo)
    {
        var alerta = new AlertaDto
        {
            Tipo = "MELHOR_VOLTA_GERAL",
            Timestamp = DateTime.UtcNow,
            IdEtapa = idEtapa,
            NumeroMoto = numeroMoto,
            NomePiloto = nomePiloto,
            Mensagem = $"🏆 MELHOR VOLTA! #{numeroMoto} {nomePiloto} - {tempo}",
            Detalhe = tempo,
            Prioridade = "ALTA"
        };

        var grupoEtapa = $"Etapa_{idEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("Alerta", alerta);

        _logger.LogInformation(
            "Alerta de melhor volta geral: Etapa {Etapa}, Moto {Moto}, Tempo {Tempo}",
            idEtapa, numeroMoto, tempo);
    }

    public async Task NotificarMelhorVoltaCategoriaAsync(
        int idEtapa,
        int idCategoria,
        int numeroMoto,
        string nomePiloto,
        string tempo)
    {
        var alerta = new AlertaDto
        {
            Tipo = "MELHOR_VOLTA_CATEGORIA",
            Timestamp = DateTime.UtcNow,
            IdEtapa = idEtapa,
            NumeroMoto = numeroMoto,
            NomePiloto = nomePiloto,
            Mensagem = $"⭐ Melhor volta da categoria! #{numeroMoto} {nomePiloto} - {tempo}",
            Detalhe = tempo,
            Prioridade = "MEDIA"
        };

        var grupoCategoria = $"Etapa_{idEtapa}_Categoria_{idCategoria}";
        await _hubContext.Clients.Group(grupoCategoria)
            .SendAsync("Alerta", alerta);
    }

    public async Task NotificarAbandonoAsync(
        int idEtapa,
        int numeroMoto,
        string nomePiloto)
    {
        var alerta = new AlertaDto
        {
            Tipo = "ABANDONO",
            Timestamp = DateTime.UtcNow,
            IdEtapa = idEtapa,
            NumeroMoto = numeroMoto,
            NomePiloto = nomePiloto,
            Mensagem = $"⚠️ Abandono: #{numeroMoto} {nomePiloto}",
            Prioridade = "MEDIA"
        };

        var grupoEtapa = $"Etapa_{idEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("Alerta", alerta);
    }

    #endregion

    #region Status da Prova

    public async Task NotificarStatusProvaAsync(int idEtapa, string status)
    {
        var statusDto = new StatusProvaDto
        {
            Tipo = "STATUS_PROVA",
            IdEtapa = idEtapa,
            Status = status
        };

        var grupoEtapa = $"Etapa_{idEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("StatusProva", statusDto);
    }

    public async Task NotificarLargadaAsync(int idEtapa, DateTime horaLargada)
    {
        var alerta = new AlertaDto
        {
            Tipo = "LARGADA",
            Timestamp = horaLargada,
            IdEtapa = idEtapa,
            Mensagem = "🏁 LARGADA!",
            Prioridade = "ALTA"
        };

        var grupoEtapa = $"Etapa_{idEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("Alerta", alerta);

        await NotificarStatusProvaAsync(idEtapa, "EM_ANDAMENTO");
    }

    public async Task NotificarBandeiraAsync(int idEtapa, DateTime horaBandeira)
    {
        var alerta = new AlertaDto
        {
            Tipo = "BANDEIRA",
            Timestamp = horaBandeira,
            IdEtapa = idEtapa,
            Mensagem = "🏁 BANDEIRA QUADRICULADA!",
            Prioridade = "ALTA"
        };

        var grupoEtapa = $"Etapa_{idEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("Alerta", alerta);

        await NotificarStatusProvaAsync(idEtapa, "BANDEIRA");
    }

    public async Task NotificarFimProvaAsync(int idEtapa)
    {
        var alerta = new AlertaDto
        {
            Tipo = "FIM_PROVA",
            Timestamp = DateTime.UtcNow,
            IdEtapa = idEtapa,
            Mensagem = "🏆 PROVA FINALIZADA!",
            Prioridade = "ALTA"
        };

        var grupoEtapa = $"Etapa_{idEtapa}";
        await _hubContext.Clients.Group(grupoEtapa)
            .SendAsync("Alerta", alerta);

        await NotificarStatusProvaAsync(idEtapa, "FINALIZADA");

        // Enviar classificação final
        await NotificarClassificacaoAtualizadaAsync(idEtapa);
    }

    #endregion

    #region Genérico

    public async Task EnviarParaGrupoAsync(string grupo, string metodo, object dados)
    {
        await _hubContext.Clients.Group(grupo).SendAsync(metodo, dados);
    }

    public async Task EnviarParaTodosAsync(string metodo, object dados)
    {
        await _hubContext.Clients.All.SendAsync(metodo, dados);
    }

    #endregion

    #region Helpers

    private string FormatarTempo(decimal segundos)
    {
        var ts = TimeSpan.FromSeconds((double)segundos);
        if (ts.TotalMinutes >= 1)
        {
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }
        return $"{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    #endregion
}
