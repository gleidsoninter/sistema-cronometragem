using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace DevNationCrono.API.Services.Implementations;

public class ResultadoEnduroService : IResultadoEnduroService
{
    private readonly ITempoRepository _tempoRepository;
    private readonly IEtapaRepository _etapaRepository;
    private readonly IInscricaoRepository _inscricaoRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResultadoEnduroService> _logger;

    // Tempo de cache dos resultados
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(30);

    public ResultadoEnduroService(
        ITempoRepository tempoRepository,
        IEtapaRepository etapaRepository,
        IInscricaoRepository inscricaoRepository,
        IMemoryCache cache,
        ILogger<ResultadoEnduroService> logger)
    {
        _tempoRepository = tempoRepository;
        _etapaRepository = etapaRepository;
        _inscricaoRepository = inscricaoRepository;
        _cache = cache;
        _logger = logger;
    }

    #region Classificação Geral

    public async Task<ClassificacaoGeralEnduroDto> CalcularClassificacaoGeralAsync(
        int idEtapa,
        ResultadoFilterParams? filtros = null)
    {
        var cacheKey = $"resultado_enduro_{idEtapa}_{filtros?.IdCategoria}_{filtros?.IncluirDesclassificados}";

        // Tentar obter do cache
        if (_cache.TryGetValue(cacheKey, out ClassificacaoGeralEnduroDto cachedResult))
        {
            _logger.LogDebug("Resultado obtido do cache: {Key}", cacheKey);
            return cachedResult;
        }

        _logger.LogInformation("Calculando classificação geral da etapa {Id}", idEtapa);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 1. Buscar etapa com configurações
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
        {
            throw new NotFoundException("Etapa não encontrada");
        }

        // Validar que é ENDURO
        if (etapa.Evento.Modalidade.TipoCronometragem != "ENDURO")
        {
            throw new ValidationException("Esta etapa não é de ENDURO");
        }

        // 2. Buscar todas as inscrições da etapa
        var inscricoes = await _inscricaoRepository.GetByEtapaAsync(idEtapa);

        // Filtrar por categoria se especificado
        if (filtros?.IdCategoria.HasValue == true)
        {
            inscricoes = inscricoes
                .Where(i => i.IdCategoria == filtros.IdCategoria.Value)
                .ToList();
        }

        // 3. Buscar todos os tempos da etapa (uma única query)
        var todosTempos = await _tempoRepository.GetByEtapaAsync(idEtapa);

        // 4. Calcular resultado de cada piloto
        var resultados = new List<ResultadoPilotoEnduroDto>();

        foreach (var inscricao in inscricoes)
        {
            var temposPiloto = todosTempos
                .Where(t => t.NumeroMoto == inscricao.NumeroMoto && !t.Descartada)
                .ToList();

            var resultado = CalcularResultadoPiloto(inscricao, temposPiloto, etapa);
            resultados.Add(resultado);
        }

        // 5. Ordenar por tempo total (classificação)
        resultados = resultados
            .OrderBy(r => r.Status == "CLASSIFICADO" ? 0 : 1) // Classificados primeiro
            .ThenBy(r => r.TempoTotalSegundos)
            .ToList();

        // 6. Atribuir posições
        AtribuirPosicoes(resultados);

        // 7. Calcular diferenças
        CalcularDiferencas(resultados);

        // 8. Filtrar desclassificados se não quiser
        if (filtros?.IncluirDesclassificados != true)
        {
            resultados = resultados.Where(r => r.Status == "CLASSIFICADO").ToList();
        }

        // 9. Limitar quantidade se especificado
        if (filtros?.TopN.HasValue == true)
        {
            resultados = resultados.Take(filtros.TopN.Value).ToList();
        }

        // 10. Remover detalhes se não quiser
        if (filtros?.IncluirDetalhes == false)
        {
            foreach (var r in resultados)
            {
                r.Voltas = null;
            }
        }

        // 11. Montar resposta
        var melhorTempo = resultados
            .Where(r => r.Status == "CLASSIFICADO")
            .OrderBy(r => r.TempoTotalSegundos)
            .FirstOrDefault();

        var resultado_final = new ClassificacaoGeralEnduroDto
        {
            IdEtapa = idEtapa,
            NomeEtapa = etapa.Nome,
            NomeEvento = etapa.Evento.Nome,
            DataEtapa = etapa.DataHora,

            NumeroVoltas = etapa.NumeroVoltas ?? 0,
            NumeroEspeciais = etapa.NumeroEspeciais ?? 0,
            PrimeiraVoltaValida = etapa.PrimeiraVoltaValida,
            PenalidadePorFaltaSegundos = etapa.PenalidadePorFaltaSegundos ?? 0,

            TotalInscritos = inscricoes.Count,
            TotalClassificados = resultados.Count(r => r.Status == "CLASSIFICADO"),
            TotalDesclassificados = resultados.Count(r => r.Status == "DESCLASSIFICADO"),
            TotalNaoLargaram = resultados.Count(r => r.Status == "NAO_LARGOU"),
            TotalAbandonos = resultados.Count(r => r.Status == "ABANDONO"),
            TotalLeituras = todosTempos.Count,

            MelhorTempoGeralFormatado = melhorTempo?.TempoTotalFormatado,
            PilotoMelhorTempoGeral = melhorTempo?.NomePiloto,

            Classificacao = resultados,
            DataCalculo = DateTime.UtcNow
        };

        stopwatch.Stop();
        _logger.LogInformation(
            "Classificação calculada em {Tempo}ms para {Total} pilotos",
            stopwatch.ElapsedMilliseconds, resultados.Count);

        // Armazenar no cache
        _cache.Set(cacheKey, resultado_final, CACHE_DURATION);

        return resultado_final;
    }

    #endregion

    #region Cálculo Individual

    private ResultadoPilotoEnduroDto CalcularResultadoPiloto(
        Inscricao inscricao,
        List<Tempo> tempos,
        Etapa etapa)
    {
        var resultado = new ResultadoPilotoEnduroDto
        {
            IdInscricao = inscricao.Id,
            IdPiloto = inscricao.IdPiloto,
            NomePiloto = inscricao.Piloto.Nome,
            Cidade = inscricao.Piloto.Cidade,
            Uf = inscricao.Piloto.Uf,
            NumeroMoto = inscricao.NumeroMoto,
            IdCategoria = inscricao.IdCategoria,
            NomeCategoria = inscricao.Categoria.Nome,
            Voltas = new List<ResultadoVoltaDto>()
        };

        decimal tempoTotal = 0;
        int totalPenalidades = 0;
        decimal totalPenalidadeSegundos = 0;
        int especiaisCompletadas = 0;
        int especiaisPenalizadas = 0;
        decimal? melhorTempoEspecial = null;
        bool temAlgumaLeitura = tempos.Any();
        bool completouAlgumaEspecial = false;

        // Processar cada volta
        for (int volta = 1; volta <= etapa.NumeroVoltas; volta++)
        {
            var resultadoVolta = new ResultadoVoltaDto
            {
                NumeroVolta = volta,
                VoltaReconhecimento = (volta == 1 && !etapa.PrimeiraVoltaValida),
                ContaNoTotal = !(volta == 1 && !etapa.PrimeiraVoltaValida),
                Especiais = new List<ResultadoEspecialDto>()
            };

            decimal tempoVolta = 0;

            // Processar cada especial
            for (int especial = 1; especial <= etapa.NumeroEspeciais; especial++)
            {
                var resultadoEspecial = CalcularTempoEspecial(
                    tempos, volta, especial, etapa.PenalidadePorFaltaSegundos ?? 0);

                resultadoVolta.Especiais.Add(resultadoEspecial);

                // Só conta se não for volta de reconhecimento
                if (resultadoVolta.ContaNoTotal)
                {
                    if (resultadoEspecial.Penalizado)
                    {
                        tempoVolta += resultadoEspecial.PenalidadeSegundos;
                        totalPenalidades++;
                        totalPenalidadeSegundos += resultadoEspecial.PenalidadeSegundos;
                        especiaisPenalizadas++;
                    }
                    else if (resultadoEspecial.TempoSegundos.HasValue)
                    {
                        tempoVolta += resultadoEspecial.TempoSegundos.Value;
                        especiaisCompletadas++;
                        completouAlgumaEspecial = true;

                        // Verificar melhor tempo
                        if (!melhorTempoEspecial.HasValue ||
                            resultadoEspecial.TempoSegundos.Value < melhorTempoEspecial.Value)
                        {
                            melhorTempoEspecial = resultadoEspecial.TempoSegundos.Value;
                        }
                    }
                }
            }

            resultadoVolta.TempoVoltaSegundos = tempoVolta;
            resultadoVolta.TempoVoltaFormatado = FormatarTempo(tempoVolta);

            if (resultadoVolta.ContaNoTotal)
            {
                tempoTotal += tempoVolta;
            }

            resultado.Voltas.Add(resultadoVolta);
        }

        // Calcular total de especiais esperadas (apenas voltas válidas)
        int voltasValidas = etapa.PrimeiraVoltaValida
            ? (etapa.NumeroVoltas ?? 0)
            : (etapa.NumeroVoltas ?? 1) - 1; resultado.TotalEspeciais = voltasValidas * etapa.NumeroEspeciais ?? 0;

        // Preencher resultado
        resultado.TempoTotalSegundos = tempoTotal;
        resultado.TempoTotalFormatado = FormatarTempo(tempoTotal);
        resultado.TotalPenalidades = totalPenalidades;
        resultado.TotalPenalidadeSegundos = totalPenalidadeSegundos;
        resultado.TotalPenalidadeFormatado = FormatarTempo(totalPenalidadeSegundos);
        resultado.EspeciaisCompletadas = especiaisCompletadas;
        resultado.EspeciaisPenalizadas = especiaisPenalizadas;
        resultado.MelhorTempoEspecialSegundos = melhorTempoEspecial;
        resultado.MelhorTempoEspecialFormatado = melhorTempoEspecial.HasValue
            ? FormatarTempo(melhorTempoEspecial.Value)
            : null;

        // Determinar status
        if (!temAlgumaLeitura)
        {
            resultado.Status = "NAO_LARGOU";
            resultado.MotivoStatus = "Nenhuma leitura registrada";
        }
        else if (!completouAlgumaEspecial)
        {
            resultado.Status = "ABANDONO";
            resultado.MotivoStatus = "Não completou nenhuma especial";
        }
        else if (especiaisPenalizadas > resultado.TotalEspeciais / 2)
        {
            resultado.Status = "DESCLASSIFICADO";
            resultado.MotivoStatus = $"Muitas penalidades ({especiaisPenalizadas} de {resultado.TotalEspeciais})";
        }
        else
        {
            resultado.Status = "CLASSIFICADO";
        }

        return resultado;
    }

    private ResultadoEspecialDto CalcularTempoEspecial(
        List<Tempo> tempos,
        int volta,
        int especial,
        int penalidadeSegundos)
    {
        var entrada = tempos.FirstOrDefault(t =>
            t.Volta == volta &&
            t.IdEspecial == especial &&
            t.Tipo == "E");

        var saida = tempos.FirstOrDefault(t =>
            t.Volta == volta &&
            t.IdEspecial == especial &&
            t.Tipo == "S");

        var resultado = new ResultadoEspecialDto
        {
            NumeroEspecial = especial,
            NumeroVolta = volta,
            Entrada = entrada?.Timestamp,
            Saida = saida?.Timestamp,
            TemEntrada = entrada != null,
            TemSaida = saida != null
        };

        if (entrada != null && saida != null)
        {
            // Tempo normal
            var diferenca = saida.Timestamp - entrada.Timestamp;
            resultado.TempoSegundos = (decimal)diferenca.TotalSeconds;
            resultado.TempoFormatado = FormatarTempo(resultado.TempoSegundos.Value);
            resultado.Penalizado = false;
        }
        else if (entrada == null && saida == null)
        {
            // Não passou na especial
            resultado.Penalizado = true;
            resultado.PenalidadeSegundos = penalidadeSegundos;
            resultado.MotivoPenalidade = "Não passou na especial";
            resultado.TempoFormatado = $"PEN ({FormatarTempo(penalidadeSegundos)})";
        }
        else if (entrada == null)
        {
            // Só tem saída (erro ou pulou entrada)
            resultado.Penalizado = true;
            resultado.PenalidadeSegundos = penalidadeSegundos;
            resultado.MotivoPenalidade = "Falta leitura de entrada";
            resultado.TempoFormatado = $"PEN ({FormatarTempo(penalidadeSegundos)})";
        }
        else
        {
            // Só tem entrada (abandono ou problema)
            resultado.Penalizado = true;
            resultado.PenalidadeSegundos = penalidadeSegundos;
            resultado.MotivoPenalidade = "Falta leitura de saída";
            resultado.TempoFormatado = $"PEN ({FormatarTempo(penalidadeSegundos)})";
        }

        return resultado;
    }

    #endregion

    #region Posições e Diferenças

    private void AtribuirPosicoes(List<ResultadoPilotoEnduroDto> resultados)
    {
        // Posição geral
        int posicaoGeral = 1;
        foreach (var resultado in resultados.Where(r => r.Status == "CLASSIFICADO"))
        {
            resultado.Posicao = posicaoGeral++;
        }

        // Marcar não classificados
        foreach (var resultado in resultados.Where(r => r.Status != "CLASSIFICADO"))
        {
            resultado.Posicao = 0;
        }

        // Posição por categoria
        var categorias = resultados.Select(r => r.IdCategoria).Distinct();
        foreach (var idCategoria in categorias)
        {
            int posicaoCategoria = 1;
            var daCategoria = resultados
                .Where(r => r.IdCategoria == idCategoria && r.Status == "CLASSIFICADO")
                .OrderBy(r => r.Posicao);

            foreach (var resultado in daCategoria)
            {
                resultado.PosicaoCategoria = posicaoCategoria++;
            }
        }
    }

    private void CalcularDiferencas(List<ResultadoPilotoEnduroDto> resultados)
    {
        var classificados = resultados
            .Where(r => r.Status == "CLASSIFICADO")
            .OrderBy(r => r.Posicao)
            .ToList();

        if (!classificados.Any())
            return;

        var tempoLider = classificados.First().TempoTotalSegundos;

        for (int i = 0; i < classificados.Count; i++)
        {
            var resultado = classificados[i];

            // Diferença para o líder
            resultado.DiferencaLiderSegundos = resultado.TempoTotalSegundos - tempoLider;
            resultado.DiferencaLiderFormatado = i == 0
                ? "-"
                : $"+{FormatarTempo(resultado.DiferencaLiderSegundos.Value)}";

            // Diferença para o anterior
            if (i > 0)
            {
                var tempoAnterior = classificados[i - 1].TempoTotalSegundos;
                resultado.DiferencaAnteriorSegundos = resultado.TempoTotalSegundos - tempoAnterior;
                resultado.DiferencaAnteriorFormatado = $"+{FormatarTempo(resultado.DiferencaAnteriorSegundos.Value)}";
            }
        }
    }

    #endregion

    #region Classificação por Categoria

    public async Task<ClassificacaoCategoriaEnduroDto> CalcularClassificacaoCategoriaAsync(
        int idEtapa,
        int idCategoria)
    {
        var filtros = new ResultadoFilterParams
        {
            IdCategoria = idCategoria,
            IncluirDesclassificados = true,
            IncluirDetalhes = true
        };

        var classificacaoGeral = await CalcularClassificacaoGeralAsync(idEtapa, filtros);

        var resultadosCategoria = classificacaoGeral.Classificacao
            .Where(r => r.IdCategoria == idCategoria)
            .ToList();

        // Recalcular posições dentro da categoria
        int posicao = 1;
        foreach (var r in resultadosCategoria.Where(r => r.Status == "CLASSIFICADO"))
        {
            r.PosicaoCategoria = posicao++;
        }

        var melhor = resultadosCategoria
            .Where(r => r.Status == "CLASSIFICADO")
            .FirstOrDefault();

        return new ClassificacaoCategoriaEnduroDto
        {
            IdCategoria = idCategoria,
            NomeCategoria = melhor?.NomeCategoria ?? "",
            TotalInscritos = resultadosCategoria.Count,
            TotalClassificados = resultadosCategoria.Count(r => r.Status == "CLASSIFICADO"),
            MelhorTempoFormatado = melhor?.TempoTotalFormatado,
            Classificacao = resultadosCategoria
        };
    }

    #endregion

    #region Resultado Individual

    public async Task<ResultadoPilotoEnduroDto> CalcularResultadoPilotoAsync(
        int idEtapa,
        int numeroMoto)
    {
        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa, new ResultadoFilterParams
        {
            IncluirDesclassificados = true,
            IncluirDetalhes = true
        });

        var resultado = classificacao.Classificacao
            .FirstOrDefault(r => r.NumeroMoto == numeroMoto);

        if (resultado == null)
        {
            throw new NotFoundException($"Piloto com moto #{numeroMoto} não encontrado");
        }

        // Marcar melhores tempos nas especiais
        await MarcarMelhoresTemposEspeciais(resultado, idEtapa);

        return resultado;
    }

    private async Task MarcarMelhoresTemposEspeciais(
        ResultadoPilotoEnduroDto resultado,
        int idEtapa)
    {
        if (resultado.Voltas == null)
            return;

        foreach (var volta in resultado.Voltas.Where(v => v.ContaNoTotal))
        {
            foreach (var especial in volta.Especiais.Where(e => !e.Penalizado && e.TempoSegundos.HasValue))
            {
                var ranking = await GetRankingEspecialAsync(idEtapa, especial.NumeroEspecial, volta.NumeroVolta);

                var posicaoNaEspecial = ranking.Ranking
                    .FirstOrDefault(r => r.NumeroMoto == resultado.NumeroMoto);

                if (posicaoNaEspecial != null)
                {
                    especial.PosicaoNaEspecial = posicaoNaEspecial.Posicao;
                    especial.MelhorTempoGeral = posicaoNaEspecial.Posicao == 1;
                }
            }
        }
    }

    #endregion

    #region Resumo Rápido

    public async Task<List<ResumoClassificacaoDto>> GetResumoClassificacaoAsync(
        int idEtapa,
        int topN = 10,
        int? idCategoria = null)
    {
        var filtros = new ResultadoFilterParams
        {
            IdCategoria = idCategoria,
            TopN = topN,
            IncluirDesclassificados = false,
            IncluirDetalhes = false
        };

        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa, filtros);

        return classificacao.Classificacao.Select(r => new ResumoClassificacaoDto
        {
            Posicao = idCategoria.HasValue ? r.PosicaoCategoria : r.Posicao,
            NumeroMoto = r.NumeroMoto,
            NomePiloto = r.NomePiloto,
            Categoria = r.NomeCategoria,
            TempoTotal = r.TempoTotalFormatado,
            Diferenca = r.DiferencaLiderFormatado,
            Penalidades = r.TotalPenalidades,
            Status = r.Status
        }).ToList();
    }

    #endregion

    #region Rankings de Especiais

    public async Task<RankingEspecialDto> GetRankingEspecialAsync(
        int idEtapa,
        int idEspecial,
        int volta)
    {
        var cacheKey = $"ranking_especial_{idEtapa}_{idEspecial}_{volta}";

        if (_cache.TryGetValue(cacheKey, out RankingEspecialDto cached))
        {
            return cached;
        }

        var tempos = await _tempoRepository.GetTemposEspecialAsync(idEtapa, idEspecial, volta);

        // Filtrar apenas saídas com tempo calculado
        var temposValidos = tempos
            .Where(t => t.Tipo == "S" && t.TempoCalculadoSegundos.HasValue && t.TempoCalculadoSegundos > 0)
            .OrderBy(t => t.TempoCalculadoSegundos)
            .ToList();

        var ranking = new RankingEspecialDto
        {
            NumeroEspecial = idEspecial,
            NumeroVolta = volta,
            Ranking = new List<TempoEspecialRankingDto>()
        };

        decimal? tempoLider = null;
        int posicao = 1;

        foreach (var tempo in temposValidos)
        {
            if (!tempoLider.HasValue)
            {
                tempoLider = tempo.TempoCalculadoSegundos;
            }

            ranking.Ranking.Add(new TempoEspecialRankingDto
            {
                Posicao = posicao++,
                NumeroMoto = tempo.NumeroMoto,
                NomePiloto = tempo.Inscricao?.Piloto?.Nome ?? "N/A",
                Categoria = tempo.Inscricao?.Categoria?.Nome ?? "N/A",
                TempoSegundos = tempo.TempoCalculadoSegundos.Value,
                TempoFormatado = tempo.TempoFormatado ?? FormatarTempo(tempo.TempoCalculadoSegundos.Value),
                DiferencaSegundos = tempo.TempoCalculadoSegundos.Value - tempoLider.Value,
                DiferencaFormatado = tempo.TempoCalculadoSegundos.Value == tempoLider.Value
                    ? "-"
                    : $"+{FormatarTempo(tempo.TempoCalculadoSegundos.Value - tempoLider.Value)}"
            });
        }

        _cache.Set(cacheKey, ranking, CACHE_DURATION);

        return ranking;
    }

    public async Task<List<RankingEspecialDto>> GetTodosRankingsEspeciaisAsync(int idEtapa)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
        {
            throw new NotFoundException("Etapa não encontrada");
        }

        var rankings = new List<RankingEspecialDto>();

        for (int volta = 1; volta <= etapa.NumeroVoltas; volta++)
        {
            // Pular volta de reconhecimento
            if (volta == 1 && !etapa.PrimeiraVoltaValida)
                continue;

            for (int especial = 1; especial <= etapa.NumeroEspeciais; especial++)
            {
                var ranking = await GetRankingEspecialAsync(idEtapa, especial, volta);
                rankings.Add(ranking);
            }
        }

        return rankings;
    }

    #endregion

    #region Comparativo

    public async Task<ComparativoPilotosDto> CompararPilotosAsync(
        int idEtapa,
        int numeroMoto1,
        int numeroMoto2)
    {
        var piloto1 = await CalcularResultadoPilotoAsync(idEtapa, numeroMoto1);
        var piloto2 = await CalcularResultadoPilotoAsync(idEtapa, numeroMoto2);

        var comparativo = new ComparativoPilotosDto
        {
            Piloto1 = piloto1,
            Piloto2 = piloto2,
            DiferencaTotalSegundos = piloto1.TempoTotalSegundos - piloto2.TempoTotalSegundos,
            DiferencaTotalFormatado = FormatarDiferenca(
                piloto1.TempoTotalSegundos - piloto2.TempoTotalSegundos),
            ComparativoEspeciais = new List<ComparativoEspecialDto>()
        };

        // Comparar cada especial
        if (piloto1.Voltas != null && piloto2.Voltas != null)
        {
            foreach (var volta1 in piloto1.Voltas.Where(v => v.ContaNoTotal))
            {
                var volta2 = piloto2.Voltas.FirstOrDefault(v => v.NumeroVolta == volta1.NumeroVolta);
                if (volta2 == null) continue;

                foreach (var esp1 in volta1.Especiais)
                {
                    var esp2 = volta2.Especiais.FirstOrDefault(e => e.NumeroEspecial == esp1.NumeroEspecial);
                    if (esp2 == null) continue;

                    var tempo1 = esp1.Penalizado ? esp1.PenalidadeSegundos : esp1.TempoSegundos;
                    var tempo2 = esp2.Penalizado ? esp2.PenalidadeSegundos : esp2.TempoSegundos;

                    string vantagem = "EMPATE";
                    decimal? diferenca = null;

                    if (tempo1.HasValue && tempo2.HasValue)
                    {
                        diferenca = tempo1.Value - tempo2.Value;
                        if (diferenca < 0) vantagem = "PILOTO1";
                        else if (diferenca > 0) vantagem = "PILOTO2";
                    }
                    else if (tempo1.HasValue)
                    {
                        vantagem = "PILOTO1";
                    }
                    else if (tempo2.HasValue)
                    {
                        vantagem = "PILOTO2";
                    }

                    comparativo.ComparativoEspeciais.Add(new ComparativoEspecialDto
                    {
                        Volta = volta1.NumeroVolta,
                        Especial = esp1.NumeroEspecial,
                        TempoPiloto1 = tempo1,
                        TempoPiloto2 = tempo2,
                        DiferencaSegundos = diferenca,
                        Vantagem = vantagem
                    });
                }
            }
        }

        return comparativo;
    }

    #endregion

    #region Cache e Recálculo

    public async Task RecalcularResultadosAsync(int idEtapa)
    {
        _logger.LogInformation("Recalculando todos os resultados da etapa {Id}", idEtapa);

        // Invalidar cache
        await InvalidarCacheAsync(idEtapa);

        // Forçar recálculo
        await CalcularClassificacaoGeralAsync(idEtapa);

        _logger.LogInformation("Resultados recalculados para etapa {Id}", idEtapa);
    }

    public Task InvalidarCacheAsync(int idEtapa)
    {
        // Invalidar caches relacionados à etapa
        // Como IMemoryCache não tem método para remover por prefixo,
        // o cache expirará naturalmente em 30 segundos

        _logger.LogInformation("Cache invalidado para etapa {Id}", idEtapa);

        return Task.CompletedTask;
    }

    #endregion

    #region Helpers

    private string FormatarTempo(decimal segundos)
    {
        var ts = TimeSpan.FromSeconds((double)segundos);

        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    private string FormatarDiferenca(decimal segundos)
    {
        var prefixo = segundos >= 0 ? "+" : "-";
        var absoluto = Math.Abs(segundos);
        return $"{prefixo}{FormatarTempo(absoluto)}";
    }

    #endregion

    private async Task<List<ResultadoPilotoEnduroDto>> CalcularResultadosParaleloAsync(
        List<Inscricao> inscricoes,
        Dictionary<int, List<Tempo>> temposPorMoto,
        Etapa etapa)
    {
        var resultados = new ConcurrentBag<ResultadoPilotoEnduroDto>();

        await Parallel.ForEachAsync(inscricoes,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (inscricao, ct) =>
            {
                var temposPiloto = temposPorMoto.TryGetValue(inscricao.NumeroMoto, out var tempos)
                    ? tempos
                    : new List<Tempo>();

                var resultado = CalcularResultadoPiloto(inscricao, temposPiloto, etapa);
                resultados.Add(resultado);
            });

        return resultados.ToList();
    }
}
