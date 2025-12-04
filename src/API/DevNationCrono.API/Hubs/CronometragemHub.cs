using DevNationCrono.API.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace DevNationCrono.API.Hubs;

public class CronometragemHub : Hub
{
    private readonly ILogger<CronometragemHub> _logger;

    public CronometragemHub(ILogger<CronometragemHub> logger)
    {
        _logger = logger;
    }

    #region Conexão

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Cliente conectado: {ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Cliente desconectado: {ConnectionId}, Motivo: {Motivo}",
            Context.ConnectionId,
            exception?.Message ?? "Normal");

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Grupos

    /// <summary>
    /// Inscreve o cliente para receber atualizações de um evento
    /// </summary>
    public async Task InscreverEvento(int idEvento)
    {
        var grupo = $"Evento_{idEvento}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "Cliente {ConnectionId} inscrito no grupo {Grupo}",
            Context.ConnectionId, grupo);

        await Clients.Caller.SendAsync("Inscrito", new { grupo, mensagem = $"Inscrito no evento {idEvento}" });
    }

    /// <summary>
    /// Inscreve o cliente para receber atualizações de uma etapa
    /// </summary>
    public async Task InscreverEtapa(int idEtapa)
    {
        var grupo = $"Etapa_{idEtapa}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "Cliente {ConnectionId} inscrito no grupo {Grupo}",
            Context.ConnectionId, grupo);

        await Clients.Caller.SendAsync("Inscrito", new { grupo, mensagem = $"Inscrito na etapa {idEtapa}" });
    }

    /// <summary>
    /// Inscreve o cliente para receber atualizações de uma categoria
    /// </summary>
    public async Task InscreverCategoria(int idEtapa, int idCategoria)
    {
        var grupo = $"Etapa_{idEtapa}_Categoria_{idCategoria}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "Cliente {ConnectionId} inscrito no grupo {Grupo}",
            Context.ConnectionId, grupo);

        await Clients.Caller.SendAsync("Inscrito", new { grupo });
    }

    /// <summary>
    /// Inscreve o cliente para receber atualizações de um piloto específico
    /// </summary>
    public async Task InscreverPiloto(int idEtapa, int numeroMoto)
    {
        var grupo = $"Etapa_{idEtapa}_Moto_{numeroMoto}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "Cliente {ConnectionId} inscrito no grupo {Grupo}",
            Context.ConnectionId, grupo);

        await Clients.Caller.SendAsync("Inscrito", new { grupo });
    }

    /// <summary>
    /// Remove inscrição de um grupo
    /// </summary>
    public async Task Desinscrever(string grupo)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "Cliente {ConnectionId} removido do grupo {Grupo}",
            Context.ConnectionId, grupo);

        await Clients.Caller.SendAsync("Desinscrito", new { grupo });
    }

    /// <summary>
    /// Inscreve em múltiplos grupos de uma vez
    /// </summary>
    public async Task InscreverMultiplos(GrupoDto dto)
    {
        var grupos = new List<string>();

        if (dto.IdEvento.HasValue)
        {
            grupos.Add($"Evento_{dto.IdEvento}");
        }

        if (dto.IdEtapa.HasValue)
        {
            grupos.Add($"Etapa_{dto.IdEtapa}");

            if (dto.IdCategoria.HasValue)
            {
                grupos.Add($"Etapa_{dto.IdEtapa}_Categoria_{dto.IdCategoria}");
            }

            if (dto.NumeroMoto.HasValue)
            {
                grupos.Add($"Etapa_{dto.IdEtapa}_Moto_{dto.NumeroMoto}");
            }
        }

        foreach (var grupo in grupos)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
        }

        _logger.LogInformation(
            "Cliente {ConnectionId} inscrito em {Count} grupos",
            Context.ConnectionId, grupos.Count);

        await Clients.Caller.SendAsync("InscritoMultiplos", new { grupos });
    }

    #endregion

    #region Ping/Pong

    /// <summary>
    /// Teste de conexão
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
    }

    #endregion
}
