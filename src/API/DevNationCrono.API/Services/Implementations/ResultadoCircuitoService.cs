using DevNationCrono.API.Data;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevNationCrono.API.Services.Implementations;

public class ResultadoCircuitoService : IResultadoCircuitoService
{
    private readonly ApplicationDbContext _context;
    private readonly ITempoRepository _tempoRepository;
    private readonly IEtapaRepository _etapaRepository;
    private readonly IInscricaoRepository _inscricaoRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResultadoCircuitoService> _logger;
    private readonly INotificacaoTempoRealService _notificacaoService;

    // Cache curto para tempo real
    private static readonly TimeSpan CACHE_TEMPO_REAL = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CACHE_COMPLETO = TimeSpan.FromSeconds(30);

    public ResultadoCircuitoService(
        ApplicationDbContext context,
        ITempoRepository tempoRepository,
        IEtapaRepository etapaRepository,
        IInscricaoRepository inscricaoRepository,
        IMemoryCache cache,
        ILogger<ResultadoCircuitoService> logger)
    {
        _context = context;
        _tempoRepository = tempoRepository;
        _etapaRepository = etapaRepository;
        _inscricaoRepository = inscricaoRepository;
        _cache = cache;
        _logger = logger;
    }

    #region Classificação Geral

    public async Task<ClassificacaoGeralCircuitoDto> CalcularClassificacaoGeralAsync(int idEtapa)
    {
        var cacheKey = $"resultado_circuito_{idEtapa}";

        if (_cache.TryGetValue(cacheKey, out ClassificacaoGeralCircuitoDto cached))
        {
            return cached;
        }

        _logger.LogInformation("Calculando classificação de circuito para etapa {Id}", idEtapa);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 1. Buscar etapa
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        if (etapa.Evento.Modalidade.TipoCronometragem != "CIRCUITO")
            throw new ValidationException("Esta etapa não é de CIRCUITO");

        var categoriasEtapa = await _context.EtapaCategorias
            .Where(ec => ec.IdEtapa == idEtapa)
            .Select(ec => ec.IdCategoria)
            .ToListAsync();

        // Filtrar inscrições apenas das categorias desta etapa
        var inscricoes = await _inscricaoRepository.GetByEtapaAsync(idEtapa);
        inscricoes = inscricoes
            .Where(i => categoriasEtapa.Contains(i.IdCategoria))
            .ToList();

        // 3. Buscar todas as passagens
        var todasPassagens = await _tempoRepository.GetByEtapaAsync(idEtapa);
        var passagens = todasPassagens.Where(t => t.Tipo == "P" && !t.Descartada).ToList();

        // 4. Calcular resultado de cada piloto
        var resultados = new List<ResultadoPilotoCircuitoDto>();

        foreach (var inscricao in inscricoes)
        {
            var passagensPiloto = passagens
                .Where(p => p.NumeroMoto == inscricao.NumeroMoto)
                .OrderBy(p => p.Timestamp)
                .ToList();

            var resultado = CalcularResultadoPiloto(inscricao, passagensPiloto, etapa);
            resultados.Add(resultado);
        }

        // 5. Ordenar por classificação
        // REGRA: Mais voltas primeiro, depois menor tempo
        resultados = resultados
            .OrderByDescending(r => r.Status == "CORRENDO" || r.Status == "FINALIZADO" ? 1 : 0)
            .ThenByDescending(r => r.VoltasCompletadas)
            .ThenBy(r => r.TempoTotalSegundos)
            .ThenBy(r => r.MelhorVoltaSegundos ?? decimal.MaxValue)
            .ToList();

        // 6. Atribuir posições
        AtribuirPosicoes(resultados);

        // 7. Calcular diferenças
        CalcularDiferencas(resultados);

        // 8. Marcar melhores voltas
        MarcarMelhoresVoltas(resultados);

        // 9. Determinar status da prova
        var statusProva = DeterminarStatusProva(etapa, passagens);

        // 10. Encontrar melhor volta geral
        var melhorVoltaGeral = resultados
            .Where(r => r.MelhorVoltaSegundos.HasValue)
            .OrderBy(r => r.MelhorVoltaSegundos)
            .FirstOrDefault();

        // 11. Montar resposta
        var classificacao = new ClassificacaoGeralCircuitoDto
        {
            IdEtapa = idEtapa,
            NomeEtapa = etapa.Nome,
            NomeEvento = etapa.Evento.Nome,
            DataEtapa = etapa.DataHora,

            TempoProvaMinutos = etapa.TempoProvaMinutos,
            NumeroVoltasPrevistas = etapa.NumeroVoltas,

            StatusProva = statusProva,
            HoraLargada = etapa.HoraLargada,
            HoraBandeira = etapa.HoraBandeira,
            TempoDecorrido = etapa.HoraLargada.HasValue
                ? DateTime.UtcNow - etapa.HoraLargada.Value
                : null,
            VoltasLider = resultados.FirstOrDefault()?.VoltasCompletadas ?? 0,

            TotalInscritos = inscricoes.Count,
            TotalEmPista = resultados.Count(r => r.EmPista),
            TotalFinalizados = resultados.Count(r => r.Status == "FINALIZADO"),
            TotalAbandonos = resultados.Count(r => r.Status == "ABANDONO"),
            TotalPassagens = passagens.Count,

            MelhorVoltaGeralFormatado = melhorVoltaGeral?.MelhorVoltaFormatado,
            MotoMelhorVoltaGeral = melhorVoltaGeral?.NumeroMoto,
            PilotoMelhorVoltaGeral = melhorVoltaGeral?.NomePiloto,

            CategoriasEmPista = inscricoes.Select(i => i.Categoria.Nome).Distinct().ToList(),

            Classificacao = resultados,
            DataCalculo = DateTime.UtcNow
        };

        stopwatch.Stop();
        _logger.LogInformation(
            "Classificação calculada em {Tempo}ms para {Total} pilotos",
            stopwatch.ElapsedMilliseconds, resultados.Count);

        _cache.Set(cacheKey, classificacao, CACHE_COMPLETO);

        return classificacao;
    }

    public async Task<List<CategoriaResumoDto>> GetCategoriasEtapaAsync(int idEtapa)
    {
        return await _context.EtapaCategorias
            .Where(ec => ec.IdEtapa == idEtapa)
            .OrderBy(ec => ec.OrdemLargada)
            .Select(ec => new CategoriaResumoDto
            {
                Id = ec.Categoria.Id,
                Nome = ec.Categoria.Nome,
                // Sigla = ec.Categoria.Sigla // se existir no DTO
            })
            .ToListAsync();
    }

    #endregion

    #region Cálculo Individual

    private ResultadoPilotoCircuitoDto CalcularResultadoPiloto(
        Inscricao inscricao,
        List<Tempo> passagens,
        Etapa etapa)
    {
        var resultado = new ResultadoPilotoCircuitoDto
        {
            IdInscricao = inscricao.Id,
            IdPiloto = inscricao.IdPiloto,
            NomePiloto = inscricao.Piloto.Nome,
            Cidade = inscricao.Piloto.Cidade,
            Uf = inscricao.Piloto.Uf,
            NumeroMoto = inscricao.NumeroMoto,
            IdCategoria = inscricao.IdCategoria,
            NomeCategoria = inscricao.Categoria.Nome,
            Voltas = new List<VoltaDetalheDto>()
        };

        if (!passagens.Any())
        {
            resultado.Status = etapa.HoraLargada.HasValue ? "NAO_LARGOU" : "AGUARDANDO";
            resultado.MotivoStatus = "Nenhuma passagem registrada";
            resultado.TotalVoltas = 0;
            resultado.VoltasCompletadas = 0;
            resultado.EmPista = false;
            return resultado;
        }

        // Calcular voltas e tempos
        decimal tempoAcumulado = 0;
        decimal? melhorVolta = null;
        int voltaMelhorTempo = 0;

        for (int i = 0; i < passagens.Count; i++)
        {
            var passagem = passagens[i];
            decimal tempoVolta;

            if (i == 0)
            {
                // Primeira passagem = tempo desde largada
                if (etapa.HoraLargada.HasValue)
                {
                    tempoVolta = (decimal)(passagem.Timestamp - etapa.HoraLargada.Value).TotalSeconds;
                }
                else
                {
                    // Sem hora de largada, usar tempo calculado se existir
                    tempoVolta = passagem.TempoCalculadoSegundos ?? 0;
                }
            }
            else
            {
                // Demais voltas = diferença para passagem anterior
                tempoVolta = (decimal)(passagem.Timestamp - passagens[i - 1].Timestamp).TotalSeconds;
            }

            tempoAcumulado += tempoVolta;

            // Verificar melhor volta (ignorar primeira que inclui tempo de largada)
            if (i > 0 && (!melhorVolta.HasValue || tempoVolta < melhorVolta.Value))
            {
                melhorVolta = tempoVolta;
                voltaMelhorTempo = i + 1;
            }

            resultado.Voltas.Add(new VoltaDetalheDto
            {
                NumeroVolta = i + 1,
                Timestamp = passagem.Timestamp,
                TempoVoltaSegundos = tempoVolta,
                TempoVoltaFormatado = FormatarTempo(tempoVolta),
                TempoAcumuladoSegundos = tempoAcumulado,
                TempoAcumuladoFormatado = FormatarTempo(tempoAcumulado),
                MelhorVolta = false // Será marcado depois
            });
        }

        // Marcar melhor volta pessoal
        if (voltaMelhorTempo > 0 && resultado.Voltas.Count >= voltaMelhorTempo)
        {
            resultado.Voltas[voltaMelhorTempo - 1].MelhorVolta = true;
        }

        // Preencher resultado
        resultado.TotalVoltas = passagens.Count;
        resultado.VoltasCompletadas = passagens.Count;
        resultado.TempoTotalSegundos = tempoAcumulado;
        resultado.TempoTotalFormatado = FormatarTempo(tempoAcumulado);

        resultado.MelhorVoltaSegundos = melhorVolta;
        resultado.MelhorVoltaFormatado = melhorVolta.HasValue ? FormatarTempo(melhorVolta.Value) : null;
        resultado.VoltaMelhorTempo = voltaMelhorTempo > 0 ? voltaMelhorTempo : null;

        resultado.UltimaVoltaSegundos = resultado.Voltas.LastOrDefault()?.TempoVoltaSegundos;
        resultado.UltimaVoltaFormatado = resultado.Voltas.LastOrDefault()?.TempoVoltaFormatado;
        resultado.UltimaPassagem = passagens.LastOrDefault()?.Timestamp;

        // Calcular média (excluindo primeira volta)
        if (resultado.Voltas.Count > 1)
        {
            var temposVoltas = resultado.Voltas.Skip(1).Select(v => v.TempoVoltaSegundos);
            resultado.MediaVoltaSegundos = temposVoltas.Average();
            resultado.MediaVoltaFormatado = FormatarTempo(resultado.MediaVoltaSegundos.Value);
        }

        // Determinar status
        resultado.Status = DeterminarStatusPiloto(etapa, passagens);
        resultado.EmPista = resultado.Status == "CORRENDO";

        return resultado;
    }

    private string DeterminarStatusPiloto(Etapa etapa, List<Tempo> passagens)
    {
        if (!passagens.Any())
        {
            return etapa.HoraLargada.HasValue ? "NAO_LARGOU" : "AGUARDANDO";
        }

        if (etapa.Status == "FINALIZADA")
        {
            return "FINALIZADO";
        }

        if (etapa.HoraBandeira.HasValue)
        {
            // Prova encerrada - verificar se cruzou a linha
            var ultimaPassagem = passagens.Last().Timestamp;
            if (ultimaPassagem > etapa.HoraBandeira.Value)
            {
                return "FINALIZADO";
            }
        }

        return "CORRENDO";
    }

    private string DeterminarStatusProva(Etapa etapa, List<Tempo> passagens)
    {
        if (etapa.Status == "FINALIZADA")
            return "FINALIZADA";

        if (etapa.HoraBandeira.HasValue)
            return "BANDEIRA";

        if (etapa.HoraLargada.HasValue)
            return "EM_ANDAMENTO";

        return "NAO_INICIADA";
    }

    #endregion

    #region Posições e Diferenças

    private void AtribuirPosicoes(List<ResultadoPilotoCircuitoDto> resultados)
    {
        // Posição geral - TODOS que têm voltas completadas recebem posição
        int posicaoGeral = 1;
        foreach (var resultado in resultados.Where(r => r.VoltasCompletadas > 0))
        {
            resultado.PosicaoGeral = posicaoGeral++;
        }

        // Posição por categoria
        var categorias = resultados.Select(r => r.IdCategoria).Distinct();
        foreach (var idCategoria in categorias)
        {
            int posicaoCategoria = 1;
            var daCategoria = resultados
                .Where(r => r.IdCategoria == idCategoria && r.VoltasCompletadas > 0)
                .OrderBy(r => r.PosicaoGeral);

            foreach (var resultado in daCategoria)
            {
                resultado.PosicaoCategoria = posicaoCategoria++;
            }
        }
    }
    private void CalcularDiferencas(List<ResultadoPilotoCircuitoDto> resultados)
    {
        var classificados = resultados
            .Where(r => r.Status == "CORRENDO" || r.Status == "FINALIZADO")
            .OrderBy(r => r.PosicaoGeral)
            .ToList();

        if (!classificados.Any())
            return;

        var lider = classificados.First();

        for (int i = 0; i < classificados.Count; i++)
        {
            var resultado = classificados[i];

            // Diferença de voltas para o líder
            resultado.DiferencaVoltasLider = lider.VoltasCompletadas - resultado.VoltasCompletadas;

            if (resultado.DiferencaVoltasLider > 0)
            {
                // Está a N voltas do líder
                resultado.DiferencaLiderFormatado = $"+{resultado.DiferencaVoltasLider} volta(s)";
            }
            else if (i == 0)
            {
                resultado.DiferencaLiderFormatado = "-";
            }
            else
            {
                // Mesma volta - diferença de tempo
                resultado.DiferencaTempoLiderSegundos = resultado.TempoTotalSegundos - lider.TempoTotalSegundos;
                resultado.DiferencaLiderFormatado = $"+{FormatarTempo(resultado.DiferencaTempoLiderSegundos.Value)}";
            }

            // Diferença para o anterior
            if (i > 0)
            {
                var anterior = classificados[i - 1];
                var difVoltas = anterior.VoltasCompletadas - resultado.VoltasCompletadas;

                if (difVoltas > 0)
                {
                    resultado.DiferencaAnteriorFormatado = $"+{difVoltas} volta(s)";
                }
                else
                {
                    var difTempo = resultado.TempoTotalSegundos - anterior.TempoTotalSegundos;
                    resultado.DiferencaAnteriorFormatado = $"+{FormatarTempo(difTempo)}";
                }
            }
        }
    }

    private void MarcarMelhoresVoltas(List<ResultadoPilotoCircuitoDto> resultados)
    {
        var classificados = resultados
            .Where(r => r.MelhorVoltaSegundos.HasValue)
            .ToList();

        if (!classificados.Any())
            return;

        // Melhor volta geral
        var melhorGeral = classificados
            .OrderBy(r => r.MelhorVoltaSegundos)
            .First();
        melhorGeral.MelhorVoltaGeral = true;

        // Melhor volta por categoria
        var categorias = classificados.Select(r => r.IdCategoria).Distinct();
        foreach (var idCategoria in categorias)
        {
            var melhorCategoria = classificados
                .Where(r => r.IdCategoria == idCategoria)
                .OrderBy(r => r.MelhorVoltaSegundos)
                .First();
            melhorCategoria.MelhorVoltaCategoria = true;
        }
    }

    #endregion

    #region Classificação por Categoria

    public async Task<ClassificacaoCategoriaCircuitoDto> CalcularClassificacaoCategoriaAsync(
        int idEtapa,
        int idCategoria)
    {
        var classificacaoGeral = await CalcularClassificacaoGeralAsync(idEtapa);

        var daCategoria = classificacaoGeral.Classificacao
            .Where(r => r.IdCategoria == idCategoria)
            .ToList();

        // Renumerar posições dentro da categoria
        int pos = 1;
        foreach (var r in daCategoria.Where(r => r.Status == "CORRENDO" || r.Status == "FINALIZADO"))
        {
            r.PosicaoCategoria = pos++;
        }

        var melhor = daCategoria
            .Where(r => r.MelhorVoltaSegundos.HasValue)
            .OrderBy(r => r.MelhorVoltaSegundos)
            .FirstOrDefault();

        return new ClassificacaoCategoriaCircuitoDto
        {
            IdCategoria = idCategoria,
            NomeCategoria = daCategoria.FirstOrDefault()?.NomeCategoria ?? "",
            TotalInscritos = daCategoria.Count,
            TotalEmPista = daCategoria.Count(r => r.EmPista),
            TotalFinalizados = daCategoria.Count(r => r.Status == "FINALIZADO"),
            VoltasLiderCategoria = daCategoria.FirstOrDefault()?.VoltasCompletadas ?? 0,
            MelhorVoltaCategoriaFormatado = melhor?.MelhorVoltaFormatado,
            PilotoMelhorVoltaCategoria = melhor?.NomePiloto,
            Classificacao = daCategoria
        };
    }

    #endregion

    #region Tempo Real

    public async Task<List<ResumoTempoRealDto>> GetResumoTempoRealAsync(
        int idEtapa,
        int? idCategoria = null)
    {
        var cacheKey = $"tempo_real_{idEtapa}_{idCategoria}";

        if (_cache.TryGetValue(cacheKey, out List<ResumoTempoRealDto> cached))
        {
            return cached;
        }

        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa);

        var resultados = classificacao.Classificacao.AsEnumerable();

        if (idCategoria.HasValue)
        {
            resultados = resultados.Where(r => r.IdCategoria == idCategoria.Value);
        }

        var resumo = resultados.Select(r => new ResumoTempoRealDto
        {
            PosicaoGeral = r.PosicaoGeral,
            PosicaoCategoria = r.PosicaoCategoria,
            NumeroMoto = r.NumeroMoto,
            NomePiloto = r.NomePiloto,
            Categoria = r.NomeCategoria,
            Voltas = r.VoltasCompletadas,
            TempoTotal = r.TempoTotalFormatado,
            Diferenca = r.DiferencaLiderFormatado,
            UltimaVolta = r.UltimaVoltaFormatado ?? "-",
            MelhorVolta = r.MelhorVoltaFormatado ?? "-",
            Status = r.Status,
            EmPista = r.EmPista
        }).ToList();

        _cache.Set(cacheKey, resumo, CACHE_TEMPO_REAL);

        return resumo;
    }

    public async Task<List<PassagemRecente>> GetUltimasPassagensAsync(int idEtapa, int quantidade = 10)
    {
        var cacheKey = $"passagens_recentes_{idEtapa}";

        if (_cache.TryGetValue(cacheKey, out List<PassagemRecente> cached))
        {
            return cached;
        }

        var passagens = await _tempoRepository.GetByEtapaAsync(idEtapa);
        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa);

        // Melhor volta geral atual
        var melhorVoltaGeral = classificacao.Classificacao
            .Where(r => r.MelhorVoltaSegundos.HasValue)
            .Min(r => r.MelhorVoltaSegundos);

        var recentes = passagens
            .Where(p => p.Tipo == "P" && !p.Descartada)
            .OrderByDescending(p => p.Timestamp)
            .Take(quantidade)
            .Select(p =>
            {
                var piloto = classificacao.Classificacao
                    .FirstOrDefault(c => c.NumeroMoto == p.NumeroMoto);

                return new PassagemRecente
                {
                    Timestamp = p.Timestamp,
                    NumeroMoto = p.NumeroMoto,
                    NomePiloto = piloto?.NomePiloto ?? "N/A",
                    Categoria = piloto?.NomeCategoria ?? "N/A",
                    Volta = p.Volta,
                    TempoVolta = p.TempoFormatado ?? "-",
                    PosicaoAtual = piloto?.PosicaoGeral ?? 0,
                    MelhorVoltaPessoal = p.MelhorVolta,
                    MelhorVoltaGeral = p.TempoCalculadoSegundos.HasValue &&
                                      p.TempoCalculadoSegundos == melhorVoltaGeral
                };
            })
            .ToList();

        _cache.Set(cacheKey, recentes, TimeSpan.FromSeconds(2));

        return recentes;
    }

    #endregion

    #region Análise de Desempenho

    public async Task<AnaliseDesempenhoDto> GetAnaliseDesempenhoAsync(int idEtapa, int numeroMoto)
    {
        var resultado = await GetResultadoPilotoAsync(idEtapa, numeroMoto);

        if (resultado.Voltas == null || resultado.Voltas.Count < 2)
        {
            throw new ValidationException("Dados insuficientes para análise");
        }

        // Ignorar primeira volta (inclui tempo de largada)
        var tempos = resultado.Voltas.Skip(1).Select(v => v.TempoVoltaSegundos).ToList();

        var melhor = tempos.Min();
        var pior = tempos.Max();
        var media = tempos.Average();

        // Calcular desvio padrão
        var somaQuadrados = tempos.Sum(t => Math.Pow((double)(t - media), 2));
        var desvioPadrao = (decimal)Math.Sqrt(somaQuadrados / tempos.Count);

        // Índice de consistência (coeficiente de variação)
        var indiceConsistencia = media > 0 ? (desvioPadrao / media) * 100 : 0;

        // Classificação de consistência
        string classificacaoConsistencia;
        if (indiceConsistencia < 2)
            classificacaoConsistencia = "EXCELENTE";
        else if (indiceConsistencia < 5)
            classificacaoConsistencia = "BOM";
        else if (indiceConsistencia < 10)
            classificacaoConsistencia = "REGULAR";
        else
            classificacaoConsistencia = "IRREGULAR";

        // Tendência (comparar primeira e última metade)
        string tendencia = "ESTAVEL";
        if (tempos.Count >= 4)
        {
            var metade = tempos.Count / 2;
            var mediaPrimeira = tempos.Take(metade).Average();
            var mediaSegunda = tempos.Skip(metade).Average();

            var variacao = ((mediaSegunda - mediaPrimeira) / mediaPrimeira) * 100;

            if (variacao < -3)
                tendencia = "MELHORANDO";
            else if (variacao > 3)
                tendencia = "PIORANDO";
        }

        // Ranking de melhor volta
        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa);
        var rankingMelhorVolta = classificacao.Classificacao
            .Where(r => r.MelhorVoltaSegundos.HasValue)
            .OrderBy(r => r.MelhorVoltaSegundos)
            .ToList()
            .FindIndex(r => r.NumeroMoto == numeroMoto) + 1;

        // Média do campo
        var mediasCampo = classificacao.Classificacao
            .Where(r => r.MediaVoltaSegundos.HasValue)
            .Select(r => r.MediaVoltaSegundos.Value)
            .ToList();

        decimal? difMediaCampo = null;
        if (mediasCampo.Any())
        {
            var mediaCampo = mediasCampo.Average();
            difMediaCampo = media - mediaCampo;
        }

        return new AnaliseDesempenhoDto
        {
            NumeroMoto = numeroMoto,
            NomePiloto = resultado.NomePiloto,
            Categoria = resultado.NomeCategoria,
            MelhorVoltaSegundos = melhor,
            PiorVoltaSegundos = pior,
            MediaVoltaSegundos = media,
            DesvioPadrao = desvioPadrao,
            MelhorVoltaFormatado = FormatarTempo(melhor),
            PiorVoltaFormatado = FormatarTempo(pior),
            MediaFormatado = FormatarTempo(media),
            IndiceConsistencia = indiceConsistencia,
            ClassificacaoConsistencia = classificacaoConsistencia,
            TemposPorVolta = tempos,
            Tendencia = tendencia,
            DiferencaMediaCampo = difMediaCampo,
            RankingMelhorVolta = rankingMelhorVolta
        };
    }

    public async Task<List<AnaliseDesempenhoDto>> GetRankingMelhorVoltaAsync(
        int idEtapa,
        int? idCategoria = null)
    {
        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa);

        var resultados = classificacao.Classificacao.AsEnumerable();

        if (idCategoria.HasValue)
        {
            resultados = resultados.Where(r => r.IdCategoria == idCategoria.Value);
        }

        return resultados
            .Where(r => r.MelhorVoltaSegundos.HasValue)
            .OrderBy(r => r.MelhorVoltaSegundos)
            .Select((r, i) => new AnaliseDesempenhoDto
            {
                NumeroMoto = r.NumeroMoto,
                NomePiloto = r.NomePiloto,
                Categoria = r.NomeCategoria,
                MelhorVoltaSegundos = r.MelhorVoltaSegundos.Value,
                MelhorVoltaFormatado = r.MelhorVoltaFormatado,
                MediaVoltaSegundos = r.MediaVoltaSegundos ?? 0,
                MediaFormatado = r.MediaVoltaFormatado ?? "-",
                RankingMelhorVolta = i + 1
            })
            .ToList();
    }

    #endregion

    #region Controle da Prova

    public async Task<ControleProvaDto> GetStatusProvaAsync(int idEtapa)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        return new ControleProvaDto
        {
            IdEtapa = idEtapa,
            StatusAtual = etapa.Status,
            HoraLargada = etapa.HoraLargada,
            HoraBandeira = etapa.HoraBandeira,
            TempoProvaMinutos = etapa.TempoProvaMinutos
        };
    }

    public async Task<ControleProvaDto> IniciarProvaAsync(IniciarProvaDto dto)
    {
        var etapa = await _etapaRepository.GetByIdAsync(dto.IdEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        if (etapa.HoraLargada.HasValue)
            throw new ValidationException("Prova já foi iniciada");

        etapa.HoraLargada = dto.HoraLargada ?? DateTime.UtcNow;
        etapa.Status = "EM_ANDAMENTO";

        await _etapaRepository.UpdateAsync(etapa);
        await InvalidarCacheAsync(dto.IdEtapa);

        _logger.LogInformation("Prova iniciada: Etapa {Id} às {Hora}", dto.IdEtapa, etapa.HoraLargada);

        await _notificacaoService.NotificarLargadaAsync(dto.IdEtapa, etapa.HoraLargada.Value);

        return await GetStatusProvaAsync(dto.IdEtapa);
    }

    public async Task<ControleProvaDto> DarBandeiraAsync(EncerrarProvaDto dto)
    {
        var etapa = await _etapaRepository.GetByIdAsync(dto.IdEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        if (!etapa.HoraLargada.HasValue)
            throw new ValidationException("Prova ainda não foi iniciada");

        if (etapa.HoraBandeira.HasValue)
            throw new ValidationException("Bandeira já foi dada");

        etapa.HoraBandeira = dto.HoraBandeira ?? DateTime.UtcNow;
        etapa.Status = "BANDEIRA";

        await _etapaRepository.UpdateAsync(etapa);
        await InvalidarCacheAsync(dto.IdEtapa);

        _logger.LogInformation("Bandeira: Etapa {Id} às {Hora}", dto.IdEtapa, etapa.HoraBandeira);

        await _notificacaoService.NotificarBandeiraAsync(dto.IdEtapa, etapa.HoraBandeira.Value);

        return await GetStatusProvaAsync(dto.IdEtapa);
    }

    public async Task<ControleProvaDto> FinalizarProvaAsync(int idEtapa)
    {
        var etapa = await _etapaRepository.GetByIdAsync(idEtapa);
        if (etapa == null)
            throw new NotFoundException("Etapa não encontrada");

        etapa.Status = "FINALIZADA";

        await _etapaRepository.UpdateAsync(etapa);
        await InvalidarCacheAsync(idEtapa);

        _logger.LogInformation("Prova finalizada: Etapa {Id}", idEtapa);

        await _notificacaoService.NotificarFimProvaAsync(idEtapa);

        return await GetStatusProvaAsync(idEtapa);
    }

    #endregion

    #region Atualização Incremental

    public async Task AtualizarResultadoIncrementalAsync(int idEtapa, int numeroMoto)
    {
        // Invalida apenas caches específicos
        var cacheKeys = new[]
        {
                $"resultado_circuito_{idEtapa}",
                $"tempo_real_{idEtapa}_",
                $"passagens_recentes_{idEtapa}"
            };

        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }

        _logger.LogDebug(
            "Cache invalidado incrementalmente para etapa {Etapa}, moto {Moto}",
            idEtapa, numeroMoto);
    }

    #endregion

    #region Resultado Individual

    public async Task<ResultadoPilotoCircuitoDto> GetResultadoPilotoAsync(int idEtapa, int numeroMoto)
    {
        var classificacao = await CalcularClassificacaoGeralAsync(idEtapa);

        var resultado = classificacao.Classificacao
            .FirstOrDefault(r => r.NumeroMoto == numeroMoto);

        if (resultado == null)
        {
            throw new NotFoundException($"Piloto com moto #{numeroMoto} não encontrado");
        }

        return resultado;
    }

    #endregion

    #region Cache

    public Task InvalidarCacheAsync(int idEtapa)
    {
        _logger.LogInformation("Invalidando todos os caches da etapa {Id}", idEtapa);

        // IMemoryCache não tem remoção por prefixo
        // Em produção, usar IDistributedCache com Redis

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
        if (ts.TotalMinutes >= 1)
        {
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }
        return $"{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    #endregion
}
