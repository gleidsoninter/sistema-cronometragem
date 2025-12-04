using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace DevNationCrono.API.Services.Implementations;

public class CronometragemService : ICronometragemService
{
    private readonly ITempoRepository _tempoRepository;
    private readonly IDispositivoColetorRepository _dispositivoRepository;
    private readonly IEtapaRepository _etapaRepository;
    private readonly IInscricaoRepository _inscricaoRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<CronometragemService> _logger;
    private readonly IResultadoCircuitoService _resultadoCircuitoService;
    private readonly INotificacaoTempoRealService _notificacaoService;

    // Tolerância para detectar duplicatas (milissegundos)
    private const int TOLERANCIA_DUPLICATA_MS = 2000;

    public CronometragemService(
        ITempoRepository tempoRepository,
        IDispositivoColetorRepository dispositivoRepository,
        IEtapaRepository etapaRepository,
        IInscricaoRepository inscricaoRepository,
        ITokenService tokenService,
        ILogger<CronometragemService> logger,
        IResultadoCircuitoService resultadoCircuitoService,
        INotificacaoTempoRealService notificacaoService)
    {
        _tempoRepository = tempoRepository;
        _dispositivoRepository = dispositivoRepository;
        _etapaRepository = etapaRepository;
        _inscricaoRepository = inscricaoRepository;
        _tokenService = tokenService;
        _logger = logger;
        _resultadoCircuitoService = resultadoCircuitoService;
        _notificacaoService = notificacaoService;
    }

    #region Processar Leituras

    public async Task<LeituraResponseDto> ProcessarLeituraAsync(LeituraDto leitura)
    {
        _logger.LogInformation(
            "Processando leitura: Moto {Moto}, Tipo {Tipo}, Etapa {Etapa}, Device {Device}",
            leitura.NumeroMoto, leitura.Tipo, leitura.IdEtapa, leitura.DeviceId);

        try
        {
            //// 1. Validar dispositivo
            var dispositivo = await ValidarDispositivoAsync(leitura.DeviceId, leitura.IdEtapa);

            // 2. Validar etapa
            var etapa = await ValidarEtapaAsync(leitura.IdEtapa);

            // 3. Validar tipo de leitura para modalidade
            ValidarTipoLeitura(leitura, etapa);

            // 4. Gerar hash para detecção de duplicatas
            var hash = GerarHashLeitura(leitura);

            // 5. Verificar duplicata exata
            if (await _tempoRepository.ExisteLeituraAsync(hash))
            {
                _logger.LogWarning("Leitura duplicada detectada (hash): {Hash}", hash);
                return CriarRespostaDuplicada(leitura, "Leitura duplicada (mesmo hash)");
            }

            // 6. Verificar duplicata por proximidade de tempo
            if (await _tempoRepository.ExisteLeituraSimilarAsync(
                leitura.IdEtapa,
                leitura.NumeroMoto,
                leitura.Timestamp,
                leitura.Tipo,
                TOLERANCIA_DUPLICATA_MS))
            {
                _logger.LogWarning(
                    "Leitura duplicada detectada (proximidade): Moto {Moto}, Timestamp {Timestamp}",
                    leitura.NumeroMoto, leitura.Timestamp);
                return CriarRespostaDuplicada(leitura, $"Leitura muito próxima (< {TOLERANCIA_DUPLICATA_MS}ms)");
            }

            // 7. Buscar inscrição do piloto
            var inscricao = await BuscarInscricaoAsync(leitura.NumeroMoto, leitura.IdEtapa);

            // 8. Criar registro de tempo
            var tempo = new Tempo
            {
                IdEtapa = leitura.IdEtapa,
                IdInscricao = inscricao?.Id,
                NumeroMoto = leitura.NumeroMoto,
                Timestamp = leitura.Timestamp,
                Tipo = leitura.Tipo,
                IdEspecial = leitura.IdEspecial,
                Volta = leitura.Volta,
                IdDispositivo = dispositivo.Id,
                HashLeitura = hash,
                DadosBrutos = leitura.DadosBrutos,
                Sincronizado = true,
                DataRecebimento = DateTime.UtcNow
            };

            // 9. Calcular tempo (se aplicável)
            await CalcularTempoLeituraAsync(tempo, etapa);

            await _tempoRepository.AddAsync(tempo);

            // 10. Salvar
            if (etapa.Evento.Modalidade.TipoCronometragem == "CIRCUITO" && tempo.Tipo == "P")
            {
                await _resultadoCircuitoService.AtualizarResultadoIncrementalAsync(
                    tempo.IdEtapa, tempo.NumeroMoto);
            }

            // ===== NOTIFICAÇÕES TEMPO REAL =====
            try
            {
                var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;

                if (tipoCronometragem == "CIRCUITO")
                {
                    // Calcular posição atual
                    var classificacao = await _resultadoCircuitoService.GetResumoTempoRealAsync(leitura.IdEtapa);
                    var posicaoAtual = classificacao
                        .FirstOrDefault(c => c.NumeroMoto == leitura.NumeroMoto)?.PosicaoGeral ?? 0;

                    // Notificar nova passagem
                    await _notificacaoService.NotificarNovaPassagemAsync(tempo, inscricao, posicaoAtual);

                    // Verificar se é melhor volta
                    await VerificarENotificarMelhorVoltaAsync(tempo, leitura.IdEtapa);

                    // Notificar classificação atualizada
                    await _notificacaoService.NotificarClassificacaoAtualizadaAsync(leitura.IdEtapa);
                }
                else // ENDURO
                {
                    decimal? tempoEspecial = null;
                    if (tempo.Tipo == "S" && tempo.TempoCalculadoSegundos.HasValue)
                    {
                        tempoEspecial = tempo.TempoCalculadoSegundos;
                    }

                    await _notificacaoService.NotificarPassagemEnduroAsync(tempo, inscricao, tempoEspecial);
                }
            }
            catch (Exception ex)
            {
                // Log mas não falha a operação principal
                _logger.LogError(ex, "Erro ao enviar notificação SignalR");
            }

            // 11. Atualizar estatísticas do dispositivo
            await _dispositivoRepository.IncrementarLeiturasAsync(dispositivo.Id);

            _logger.LogInformation(
                "Leitura processada com sucesso: ID {Id}, Moto {Moto}, Tempo {Tempo}",
                tempo.Id, tempo.NumeroMoto, tempo.TempoFormatado);

            return MapearParaResponse(tempo, inscricao, "OK", "Leitura processada com sucesso");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validação falhou: {Message}", ex.Message);
            return CriarRespostaErro(leitura, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar leitura");
            return CriarRespostaErro(leitura, "Erro interno ao processar leitura");
        }
    }

    public async Task<LoteLeituraResponseDto> ProcessarLoteLeituraAsync(LoteLeituraDto lote)
    {
        _logger.LogInformation(
            "Processando lote de {Count} leituras do dispositivo {Device}",
            lote.Leituras.Count, lote.DeviceId);

        var response = new LoteLeituraResponseDto
        {
            TotalRecebidas = lote.Leituras.Count,
            Leituras = new List<LeituraResponseDto>(),
            Erros = new List<string>()
        };

        // Validar dispositivo uma vez
        DispositivoColetor dispositivo;
        try
        {
            dispositivo = await ValidarDispositivoAsync(lote.DeviceId, lote.IdEtapa);
        }
        catch (Exception ex)
        {
            response.Erros.Add($"Dispositivo inválido: {ex.Message}");
            return response;
        }

        // Ordenar por timestamp
        var leiturasOrdenadas = lote.Leituras.OrderBy(l => l.Timestamp).ToList();

        foreach (var item in leiturasOrdenadas)
        {
            var leituraDto = new LeituraDto
            {
                NumeroMoto = item.NumeroMoto,
                Timestamp = item.Timestamp,
                Tipo = item.Tipo,
                IdEspecial = item.IdEspecial,
                Volta = item.Volta,
                IdEtapa = lote.IdEtapa,
                DeviceId = lote.DeviceId,
                IdLocal = item.IdLocal,
                DadosBrutos = item.DadosBrutos
            };

            var resultado = await ProcessarLeituraAsync(leituraDto);
            response.Leituras.Add(resultado);

            switch (resultado.Status)
            {
                case "OK":
                    response.TotalProcessadas++;
                    break;
                case "DUPLICADA":
                    response.TotalDuplicadas++;
                    break;
                default:
                    response.TotalErros++;
                    response.Erros.Add($"Moto {item.NumeroMoto}: {resultado.Mensagem}");
                    break;
            }
        }

        // Atualizar status do dispositivo
        await _dispositivoRepository.AtualizarStatusConexaoAsync(dispositivo.Id, "ONLINE");

        _logger.LogInformation(
            "Lote processado: {Total} recebidas, {Processadas} OK, {Duplicadas} duplicadas, {Erros} erros",
            response.TotalRecebidas, response.TotalProcessadas,
            response.TotalDuplicadas, response.TotalErros);

        return response;
    }

    #endregion

    #region Cálculo de Tempos

    private async Task CalcularTempoLeituraAsync(Tempo tempo, Etapa etapa)
    {
        var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "ENDURO")
        {
            await CalcularTempoEnduroAsync(tempo, etapa);
        }
        else // CIRCUITO
        {
            await CalcularTempoCircuitoAsync(tempo, etapa);
        }
    }

    private async Task CalcularTempoEnduroAsync(Tempo tempo, Etapa etapa)
    {
        // No ENDURO, calculamos o tempo da especial quando recebemos a SAÍDA
        if (tempo.Tipo != "S" || !tempo.IdEspecial.HasValue)
            return;

        // Buscar entrada correspondente
        var entrada = await _tempoRepository.GetEntradaEspecialAsync(
            tempo.IdEtapa,
            tempo.NumeroMoto,
            tempo.IdEspecial.Value,
            tempo.Volta);

        if (entrada == null)
        {
            _logger.LogWarning(
                "Entrada não encontrada para saída: Moto {Moto}, Especial {Especial}, Volta {Volta}",
                tempo.NumeroMoto, tempo.IdEspecial, tempo.Volta);
            return;
        }

        // Calcular diferença
        var diferenca = tempo.Timestamp - entrada.Timestamp;
        tempo.TempoCalculadoSegundos = (decimal)diferenca.TotalSeconds;
        tempo.TempoFormatado = FormatarTempo(diferenca);

        _logger.LogInformation(
            "Tempo ENDURO calculado: Moto {Moto}, Especial {Especial}, Volta {Volta}, Tempo {Tempo}",
            tempo.NumeroMoto, tempo.IdEspecial, tempo.Volta, tempo.TempoFormatado);
    }

    private async Task CalcularTempoCircuitoAsync(Tempo tempo, Etapa etapa)
    {
        // No CIRCUITO, calculamos o tempo da volta quando recebemos uma PASSAGEM
        if (tempo.Tipo != "P")
            return;

        // Buscar passagem anterior
        var passagemAnterior = await _tempoRepository.GetUltimaPassagemAsync(
            tempo.IdEtapa, tempo.NumeroMoto);

        if (passagemAnterior == null)
        {
            // Primeira passagem - é a largada, não tem tempo de volta
            tempo.Volta = 1;
            tempo.TempoFormatado = "LARGADA";
            return;
        }

        // Determinar número da volta
        var totalVoltas = await _tempoRepository.GetTotalVoltasAsync(tempo.IdEtapa, tempo.NumeroMoto);
        tempo.Volta = totalVoltas + 1;

        // Calcular tempo da volta
        var diferenca = tempo.Timestamp - passagemAnterior.Timestamp;
        tempo.TempoCalculadoSegundos = (decimal)diferenca.TotalSeconds;
        tempo.TempoFormatado = FormatarTempo(diferenca);

        _logger.LogInformation(
            "Tempo CIRCUITO calculado: Moto {Moto}, Volta {Volta}, Tempo {Tempo}",
            tempo.NumeroMoto, tempo.Volta, tempo.TempoFormatado);
    }

    public async Task<decimal?> CalcularTempoEspecialAsync(
        int idEtapa, int numeroMoto, int idEspecial, int volta)
    {
        var entrada = await _tempoRepository.GetEntradaEspecialAsync(
            idEtapa, numeroMoto, idEspecial, volta);

        var saida = await _tempoRepository.GetSaidaEspecialAsync(
            idEtapa, numeroMoto, idEspecial, volta);

        if (entrada == null || saida == null)
            return null;

        var diferenca = saida.Timestamp - entrada.Timestamp;
        return (decimal)diferenca.TotalSeconds;
    }

    public async Task RecalcularTemposEtapaAsync(int idEtapa)
    {
        _logger.LogInformation("Recalculando tempos da etapa {Id}", idEtapa);

        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        var tempos = await _tempoRepository.GetByEtapaAsync(idEtapa);
        var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "ENDURO")
        {
            // Recalcular tempos de saída
            var saidas = tempos.Where(t => t.Tipo == "S").ToList();
            foreach (var saida in saidas)
            {
                await CalcularTempoEnduroAsync(saida, etapa);
            }
            await _tempoRepository.UpdateRangeAsync(saidas);
        }
        else
        {
            // Recalcular voltas do circuito
            var motoNumeros = tempos.Select(t => t.NumeroMoto).Distinct();
            foreach (var moto in motoNumeros)
            {
                var passagens = tempos
                    .Where(t => t.NumeroMoto == moto && t.Tipo == "P")
                    .OrderBy(t => t.Timestamp)
                    .ToList();

                for (int i = 0; i < passagens.Count; i++)
                {
                    passagens[i].Volta = i + 1;

                    if (i == 0)
                    {
                        passagens[i].TempoFormatado = "LARGADA";
                        passagens[i].TempoCalculadoSegundos = null;
                    }
                    else
                    {
                        var diferenca = passagens[i].Timestamp - passagens[i - 1].Timestamp;
                        passagens[i].TempoCalculadoSegundos = (decimal)diferenca.TotalSeconds;
                        passagens[i].TempoFormatado = FormatarTempo(diferenca);
                    }
                }

                await _tempoRepository.UpdateRangeAsync(passagens);
            }
        }

        // Recalcular melhores voltas
        await RecalcularMelhoresVoltasAsync(idEtapa);

        _logger.LogInformation("Tempos recalculados para etapa {Id}", idEtapa);
    }

    public async Task RecalcularMelhoresVoltasAsync(int idEtapa)
    {
        var tempos = await _tempoRepository.GetByEtapaAsync(idEtapa);

        // Resetar todas as melhores voltas
        foreach (var tempo in tempos)
        {
            tempo.MelhorVolta = false;
        }

        // Agrupar por piloto
        var grupos = tempos
            .Where(t => t.TempoCalculadoSegundos.HasValue && t.TempoCalculadoSegundos > 0)
            .GroupBy(t => t.NumeroMoto);

        foreach (var grupo in grupos)
        {
            var melhor = grupo
                .OrderBy(t => t.TempoCalculadoSegundos)
                .FirstOrDefault();

            if (melhor != null)
            {
                melhor.MelhorVolta = true;
            }
        }

        await _tempoRepository.UpdateRangeAsync(tempos);
    }

    #endregion

    #region Consultas

    public async Task<List<LeituraResponseDto>> GetLeiturasEtapaAsync(int idEtapa)
    {
        var tempos = await _tempoRepository.GetByEtapaAsync(idEtapa);
        return tempos.Select(t => MapearParaResponse(t, t.Inscricao, "OK", null)).ToList();
    }

    public async Task<PagedResult<LeituraResponseDto>> GetLeiturasPagedAsync(LeituraFilterParams filter)
    {
        var result = await _tempoRepository.GetPagedAsync(filter);
        var dtos = result.Items.Select(t => MapearParaResponse(t, t.Inscricao, "OK", null)).ToList();
        return new PagedResult<LeituraResponseDto>(
            dtos, result.TotalCount, result.PageNumber, result.PageSize);
    }

    public async Task<List<TempoCalculadoDto>> GetTemposPilotoAsync(int idEtapa, int numeroMoto)
    {
        var tempos = await _tempoRepository.GetByNumeroMotoEtapaAsync(numeroMoto, idEtapa);
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);

        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;
        var result = new List<TempoCalculadoDto>();

        if (tipoCronometragem == "ENDURO")
        {
            // Agrupar por volta e especial
            for (int volta = 1; volta <= etapa.NumeroVoltas; volta++)
            {
                for (int especial = 1; especial <= etapa.NumeroEspeciais; especial++)
                {
                    var entrada = tempos.FirstOrDefault(t =>
                        t.Volta == volta && t.IdEspecial == especial && t.Tipo == "E");
                    var saida = tempos.FirstOrDefault(t =>
                        t.Volta == volta && t.IdEspecial == especial && t.Tipo == "S");

                    var dto = new TempoCalculadoDto
                    {
                        IdTempo = saida?.Id ?? entrada?.Id ?? 0,
                        NumeroMoto = numeroMoto,
                        NomePiloto = entrada?.Inscricao?.Piloto?.Nome ??
                                    saida?.Inscricao?.Piloto?.Nome ?? "",
                        Categoria = entrada?.Inscricao?.Categoria?.Nome ??
                                   saida?.Inscricao?.Categoria?.Nome ?? "",
                        Volta = volta,
                        IdEspecial = especial,
                        Entrada = entrada?.Timestamp,
                        Saida = saida?.Timestamp
                    };

                    if (entrada != null && saida != null)
                    {
                        var diff = saida.Timestamp - entrada.Timestamp;
                        dto.TempoSegundos = (decimal)diff.TotalSeconds;
                        dto.TempoFormatado = FormatarTempo(diff);
                    }
                    else if (entrada == null && saida == null)
                    {
                        // Piloto não passou - aplicar penalidade
                        dto.Penalizado = true;
                        dto.PenalidadeSegundos = etapa.PenalidadePorFaltaSegundos ?? 0;
                        dto.TempoSegundos = etapa.PenalidadePorFaltaSegundos ?? 0;
                        dto.TempoFormatado = FormatarTempo(
                            TimeSpan.FromSeconds(etapa.PenalidadePorFaltaSegundos ?? 0));
                        dto.MotivoPenalidade = "Não passou na especial";
                    }
                    else
                    {
                        dto.Penalizado = true;
                        dto.PenalidadeSegundos = etapa.PenalidadePorFaltaSegundos ?? 0;
                        dto.TempoSegundos = etapa.PenalidadePorFaltaSegundos;
                        dto.TempoFormatado = FormatarTempo(
                            TimeSpan.FromSeconds(etapa.PenalidadePorFaltaSegundos ?? 0));
                        dto.MotivoPenalidade = entrada == null
                            ? "Falta entrada"
                            : "Falta saída";
                    }

                    // Verificar se volta é de reconhecimento
                    if (volta == 1 && !etapa.PrimeiraVoltaValida)
                    {
                        dto.MotivoPenalidade = "Volta de reconhecimento (não conta)";
                        dto.Penalizado = false;
                        dto.PenalidadeSegundos = 0;
                    }

                    result.Add(dto);
                }
            }
        }
        else // CIRCUITO
        {
            var passagens = tempos
                .Where(t => t.Tipo == "P")
                .OrderBy(t => t.Volta)
                .ToList();

            foreach (var passagem in passagens)
            {
                result.Add(new TempoCalculadoDto
                {
                    IdTempo = passagem.Id,
                    NumeroMoto = numeroMoto,
                    NomePiloto = passagem.Inscricao?.Piloto?.Nome ?? "",
                    Categoria = passagem.Inscricao?.Categoria?.Nome ?? "",
                    Volta = passagem.Volta,
                    Saida = passagem.Timestamp,
                    TempoSegundos = passagem.TempoCalculadoSegundos,
                    TempoFormatado = passagem.TempoFormatado ?? ""
                });
            }
        }

        return result;
    }

    #endregion

    #region Correção Manual

    public async Task<LeituraResponseDto> CorrigirLeituraAsync(long id, CorrecaoTempoDto correcao)
    {
        var tempo = await _tempoRepository.GetByIdAsync(id);
        if (tempo == null)
            throw new NotFoundException("Leitura não encontrada");

        if (correcao.NumeroMoto.HasValue)
            tempo.NumeroMoto = correcao.NumeroMoto.Value;

        if (correcao.Timestamp.HasValue)
            tempo.Timestamp = correcao.Timestamp.Value;

        if (!string.IsNullOrEmpty(correcao.Tipo))
            tempo.Tipo = correcao.Tipo;

        if (correcao.IdEspecial.HasValue)
            tempo.IdEspecial = correcao.IdEspecial;

        if (correcao.Volta.HasValue)
            tempo.Volta = correcao.Volta.Value;

        tempo.ManualmenteCorrigido = true;
        tempo.DataAtualizacao = DateTime.UtcNow;

        // Recalcular tempo se necessário
        var etapa = await _etapaRepository.GetByIdAsync(tempo.IdEtapa);
        if (etapa != null)
        {
            await CalcularTempoLeituraAsync(tempo, etapa);
        }

        await _tempoRepository.UpdateAsync(tempo);

        // Buscar inscrição atualizada
        var inscricao = await BuscarInscricaoAsync(tempo.NumeroMoto, tempo.IdEtapa);
        tempo.IdInscricao = inscricao?.Id;
        await _tempoRepository.UpdateAsync(tempo);

        _logger.LogInformation("Leitura corrigida manualmente: ID {Id}", id);

        return MapearParaResponse(tempo, inscricao, "CORRIGIDO", "Leitura corrigida com sucesso");
    }

    public async Task<LeituraResponseDto> DescartarLeituraAsync(long id, string motivo)
    {
        var tempo = await _tempoRepository.GetByIdAsync(id);
        if (tempo == null)
            throw new NotFoundException("Leitura não encontrada");

        tempo.Descartada = true;
        tempo.MotivoDescarte = motivo;
        tempo.DataAtualizacao = DateTime.UtcNow;

        await _tempoRepository.UpdateAsync(tempo);

        _logger.LogInformation("Leitura descartada: ID {Id}, Motivo: {Motivo}", id, motivo);

        return MapearParaResponse(tempo, tempo.Inscricao, "DESCARTADA", motivo);
    }

    public async Task<LeituraResponseDto> RestaurarLeituraAsync(long id)
    {
        var tempo = await _tempoRepository.GetByIdAsync(id);
        if (tempo == null)
            throw new NotFoundException("Leitura não encontrada");

        tempo.Descartada = false;
        tempo.MotivoDescarte = null;
        tempo.DataAtualizacao = DateTime.UtcNow;

        await _tempoRepository.UpdateAsync(tempo);

        _logger.LogInformation("Leitura restaurada: ID {Id}", id);

        return MapearParaResponse(tempo, tempo.Inscricao, "OK", "Leitura restaurada");
    }

    #endregion

    #region Dispositivos

    public async Task<ColetorLoginResponseDto> AutenticarColetorAsync(ColetorLoginDto dto)
    {
        _logger.LogInformation(
            "Autenticando coletor: DeviceId {DeviceId}, Etapa {Etapa}",
            dto.DeviceId, dto.IdEtapa);

        var dispositivo = await _dispositivoRepository.GetByDeviceIdEtapaAsync(
            dto.DeviceId, dto.IdEtapa);

        if (dispositivo == null)
        {
            return new ColetorLoginResponseDto
            {
                Sucesso = false,
                Mensagem = "Dispositivo não cadastrado para esta etapa"
            };
        }

        if (!dispositivo.Ativo)
        {
            return new ColetorLoginResponseDto
            {
                Sucesso = false,
                Mensagem = "Dispositivo desativado"
            };
        }

        // Gerar token
        var token = _tokenService.GerarTokenColetor(dispositivo);

        // Atualizar status
        dispositivo.Token = token;
        dispositivo.StatusConexao = "ONLINE";
        dispositivo.UltimaConexao = DateTime.UtcNow;
        await _dispositivoRepository.UpdateAsync(dispositivo);

        _logger.LogInformation("Coletor autenticado: {Nome}", dispositivo.Nome);

        return new ColetorLoginResponseDto
        {
            Sucesso = true,
            Token = token,
            IdDispositivo = dispositivo.Id,
            Nome = dispositivo.Nome,
            Tipo = dispositivo.Tipo,
            IdEspecial = dispositivo.IdEspecial,
            NomeEvento = dispositivo.Evento.Nome,
            NomeEtapa = dispositivo.Etapa.Nome,
            TipoCronometragem = dispositivo.Evento.Modalidade.TipoCronometragem,
            Mensagem = "Autenticado com sucesso"
        };
    }

    public async Task AtualizarHeartbeatAsync(ColetorHeartbeatDto dto)
    {
        var dispositivo = await _dispositivoRepository.GetByDeviceIdAsync(dto.DeviceId);
        if (dispositivo == null)
            return;

        dispositivo.StatusConexao = dto.ConexaoInternet ? "ONLINE" : "OFFLINE";
        dispositivo.UltimaConexao = DateTime.UtcNow;
        await _dispositivoRepository.UpdateAsync(dispositivo);
    }

    public async Task<List<DispositivoColetorDto>> GetDispositivosEtapaAsync(int idEtapa)
    {
        var dispositivos = await _dispositivoRepository.GetByEtapaAsync(idEtapa);

        return dispositivos.Select(d => new DispositivoColetorDto
        {
            Id = d.Id,
            IdEvento = d.IdEvento,
            NomeEvento = d.Evento?.Nome ?? "",
            IdEtapa = d.IdEtapa,
            NomeEtapa = d.Etapa?.Nome ?? "",
            Nome = d.Nome,
            Tipo = d.Tipo,
            TipoDescricao = ObterDescricaoTipo(d.Tipo),
            IdEspecial = d.IdEspecial,
            DeviceId = d.DeviceId,
            StatusConexao = d.StatusConexao,
            UltimaConexao = d.UltimaConexao,
            UltimaLeitura = d.UltimaLeitura,
            TotalLeituras = d.TotalLeituras,
            Ativo = d.Ativo
        }).ToList();
    }

    #endregion

    #region Métodos Privados

    private async Task<DispositivoColetor> ValidarDispositivoAsync(string deviceId, int idEtapa)
    {
        var dispositivo = await _dispositivoRepository.GetByDeviceIdEtapaAsync(deviceId, idEtapa);

        if (dispositivo == null)
        {
            throw new ValidationException(
                $"Dispositivo '{deviceId}' não está cadastrado para esta etapa");
        }

        if (!dispositivo.Ativo)
        {
            throw new ValidationException("Dispositivo está desativado");
        }

        return dispositivo;
    }

    private async Task<Etapa> ValidarEtapaAsync(int idEtapa)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);

        if (etapa == null)
        {
            throw new ValidationException("Etapa não encontrada");
        }

        if (etapa.Status == "CANCELADA")
        {
            throw new ValidationException("Etapa foi cancelada");
        }

        if (etapa.Status == "FINALIZADA")
        {
            throw new ValidationException("Etapa já foi finalizada");
        }

        return etapa;
    }

    private void ValidarTipoLeitura(LeituraDto leitura, Etapa etapa)
    {
        var tipoCronometragem = etapa.Evento.Modalidade.TipoCronometragem;

        if (tipoCronometragem == "ENDURO")
        {
            // ENDURO aceita E (Entrada) ou S (Saída)
            if (leitura.Tipo != "E" && leitura.Tipo != "S")
            {
                throw new ValidationException(
                    "Para ENDURO, o tipo deve ser E (Entrada) ou S (Saída)");
            }

            // ENDURO requer IdEspecial
            if (!leitura.IdEspecial.HasValue || leitura.IdEspecial < 1)
            {
                throw new ValidationException(
                    "Para ENDURO, IdEspecial é obrigatório");
            }

            if (leitura.IdEspecial > etapa.NumeroEspeciais)
            {
                throw new ValidationException(
                    $"IdEspecial deve ser entre 1 e {etapa.NumeroEspeciais}");
            }

            if (leitura.Volta > etapa.NumeroVoltas)
            {
                throw new ValidationException(
                    $"Volta deve ser entre 1 e {etapa.NumeroVoltas}");
            }
        }
        else // CIRCUITO
        {
            // CIRCUITO aceita P (Passagem)
            if (leitura.Tipo != "P")
            {
                throw new ValidationException(
                    "Para CIRCUITO, o tipo deve ser P (Passagem)");
            }
        }
    }

    private string GerarHashLeitura(LeituraDto leitura)
    {
        var dados = $"{leitura.IdEtapa}|{leitura.NumeroMoto}|{leitura.Timestamp:O}|{leitura.Tipo}|{leitura.IdEspecial}|{leitura.Volta}|{leitura.DeviceId}|{leitura.IdLocal}";

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dados));
        return Convert.ToHexString(bytes);
    }

    private async Task<Inscricao?> BuscarInscricaoAsync(int numeroMoto, int idEtapa)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null) return null;

        // Buscar inscrição pelo número da moto no evento
        var inscricoes = await _inscricaoRepository.GetByEventoAsync(etapa.IdEvento);
        return inscricoes.FirstOrDefault(i =>
            i.NumeroMoto == numeroMoto &&
            i.IdEtapa == idEtapa &&
            i.Ativo &&
            i.StatusPagamento != "CANCELADO");
    }

    private string FormatarTempo(TimeSpan tempo)
    {
        if (tempo.TotalHours >= 1)
        {
            return $"{(int)tempo.TotalHours}:{tempo.Minutes:D2}:{tempo.Seconds:D2}.{tempo.Milliseconds:D3}";
        }
        return $"{tempo.Minutes:D2}:{tempo.Seconds:D2}.{tempo.Milliseconds:D3}";
    }

    private string ObterDescricaoTipo(string tipo)
    {
        return tipo switch
        {
            "E" => "Entrada",
            "S" => "Saída",
            "P" => "Passagem",
            _ => tipo
        };
    }

    private LeituraResponseDto MapearParaResponse(
        Tempo tempo,
        Inscricao? inscricao,
        string status,
        string? mensagem)
    {
        return new LeituraResponseDto
        {
            Id = tempo.Id,
            NumeroMoto = tempo.NumeroMoto,
            NomePiloto = inscricao?.Piloto?.Nome ?? "Não inscrito",
            Categoria = inscricao?.Categoria?.Nome ?? "",
            Timestamp = tempo.Timestamp,
            Tipo = tempo.Tipo,
            TipoDescricao = ObterDescricaoTipo(tempo.Tipo),
            IdEspecial = tempo.IdEspecial,
            Volta = tempo.Volta,
            TempoCalculadoSegundos = tempo.TempoCalculadoSegundos,
            TempoFormatado = tempo.TempoFormatado ?? "",
            MelhorVolta = tempo.MelhorVolta,
            Status = status,
            Mensagem = mensagem ?? "",
            Sincronizado = tempo.Sincronizado
        };
    }

    private LeituraResponseDto CriarRespostaDuplicada(LeituraDto leitura, string mensagem)
    {
        return new LeituraResponseDto
        {
            NumeroMoto = leitura.NumeroMoto,
            Timestamp = leitura.Timestamp,
            Tipo = leitura.Tipo,
            TipoDescricao = ObterDescricaoTipo(leitura.Tipo),
            IdEspecial = leitura.IdEspecial,
            Volta = leitura.Volta,
            Status = "DUPLICADA",
            Mensagem = mensagem
        };
    }

    private LeituraResponseDto CriarRespostaErro(LeituraDto leitura, string mensagem)
    {
        return new LeituraResponseDto
        {
            NumeroMoto = leitura.NumeroMoto,
            Timestamp = leitura.Timestamp,
            Tipo = leitura.Tipo,
            TipoDescricao = ObterDescricaoTipo(leitura.Tipo),
            IdEspecial = leitura.IdEspecial,
            Volta = leitura.Volta,
            Status = "ERRO",
            Mensagem = mensagem
        };
    }

    private async Task VerificarENotificarMelhorVoltaAsync(Tempo tempo, int idEtapa)
    {
        if (!tempo.TempoCalculadoSegundos.HasValue || tempo.Volta <= 1)
            return;

        // Buscar melhor volta geral atual
        var classificacao = await _resultadoCircuitoService.CalcularClassificacaoGeralAsync(idEtapa);

        var melhorVoltaGeral = classificacao.Classificacao
            .Where(c => c.MelhorVoltaSegundos.HasValue)
            .OrderBy(c => c.MelhorVoltaSegundos)
            .FirstOrDefault();

        if (melhorVoltaGeral != null &&
            melhorVoltaGeral.NumeroMoto == tempo.NumeroMoto &&
            tempo.TempoCalculadoSegundos == melhorVoltaGeral.MelhorVoltaSegundos)
        {
            // É a melhor volta geral!
            await _notificacaoService.NotificarMelhorVoltaGeralAsync(
                idEtapa,
                tempo.NumeroMoto,
                melhorVoltaGeral.NomePiloto,
                melhorVoltaGeral.MelhorVoltaFormatado);
        }
    }

    #endregion
}
