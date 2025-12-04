using System.Diagnostics;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Testing
{
    public class StressTestService
    {
        private readonly IStorageService _storageService;
        private readonly IParserService _parserService;
        private readonly ISerialService _serialService;

        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public event EventHandler<StressTestProgressEventArgs>? ProgressChanged;
        public event EventHandler<StressTestResultEventArgs>? TestCompleted;

        public bool IsRunning => _isRunning;

        public StressTestService(
            IStorageService storageService,
            IParserService parserService,
            ISerialService serialService)
        {
            _storageService = storageService;
            _parserService = parserService;
            _serialService = serialService;
        }

        /// <summary>
        /// Executa teste de stress de leituras rápidas
        /// </summary>
        public async Task<StressTestResult> ExecutarTesteLeiturasRapidasAsync(
            int totalLeituras = 100,
            int intervaloMs = 50,
            int idEtapa = 1)
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();

            var resultado = new StressTestResult
            {
                NomeTeste = "Leituras Rápidas",
                TotalEsperado = totalLeituras,
                IntervaloMs = intervaloMs
            };

            var stopwatch = Stopwatch.StartNew();
            var leiturasProcessadas = 0;
            var erros = new List<string>();
            var temposProcessamento = new List<double>();

            try
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StressTest] Iniciando: {totalLeituras} leituras a cada {intervaloMs}ms");

                for (int i = 1; i <= totalLeituras && !_cts.Token.IsCancellationRequested; i++)
                {
                    var swLeitura = Stopwatch.StartNew();

                    try
                    {
                        // Simular dados do coletor
                        var numeroMoto = (i % 99) + 1;
                        var timestamp = DateTime.UtcNow;
                        var dados = $"{numeroMoto},{timestamp:yyyy-MM-dd HH:mm:ss.fff}";

                        // Parsear
                        var parseResult = _parserService.Parsear(dados, "GENERICO");

                        if (!parseResult.Sucesso)
                        {
                            erros.Add($"Leitura {i}: Falha no parsing - {parseResult.Erro}");
                            continue;
                        }

                        var leitura = parseResult.Leitura!;
                        leitura.IdEtapa = idEtapa;
                        leitura.DeviceId = "STRESS-TEST";
                        leitura.DadosBrutos = dados;
                        leitura.GerarHash();

                        // Validar
                        var validResult = _parserService.Validar(leitura);

                        if (!validResult.IsValid)
                        {
                            erros.Add($"Leitura {i}: Falha na validação - {string.Join(", ", validResult.Erros)}");
                            continue;
                        }

                        // Salvar
                        await _storageService.SalvarLeituraAsync(leitura);

                        swLeitura.Stop();
                        temposProcessamento.Add(swLeitura.Elapsed.TotalMilliseconds);
                        leiturasProcessadas++;

                        // Reportar progresso
                        if (i % 10 == 0)
                        {
                            ProgressChanged?.Invoke(this, new StressTestProgressEventArgs
                            {
                                Atual = i,
                                Total = totalLeituras,
                                Percentual = (double)i / totalLeituras * 100
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        erros.Add($"Leitura {i}: Exceção - {ex.Message}");
                    }

                    // Aguardar intervalo
                    if (intervaloMs > 0 && i < totalLeituras)
                    {
                        await Task.Delay(intervaloMs, _cts.Token);
                    }
                }

                stopwatch.Stop();

                // Calcular métricas
                resultado.TotalProcessado = leiturasProcessadas;
                resultado.TotalErros = erros.Count;
                resultado.TempoTotalMs = stopwatch.ElapsedMilliseconds;
                resultado.Erros = erros;

                if (temposProcessamento.Count > 0)
                {
                    resultado.TempoMedioProcessamentoMs = temposProcessamento.Average();
                    resultado.TempoMaximoProcessamentoMs = temposProcessamento.Max();
                    resultado.TempoMinimoProcessamentoMs = temposProcessamento.Min();
                }

                resultado.LeiturasSegundo = leiturasProcessadas / (stopwatch.ElapsedMilliseconds / 1000.0);
                resultado.Sucesso = erros.Count == 0;

                System.Diagnostics.Debug.WriteLine(
                    $"[StressTest] Concluído: {leiturasProcessadas}/{totalLeituras} em {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException)
            {
                resultado.Cancelado = true;
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Erros.Add($"Erro fatal: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                TestCompleted?.Invoke(this, new StressTestResultEventArgs { Resultado = resultado });
            }

            return resultado;
        }

        /// <summary>
        /// Teste de rajada (burst) - muitas leituras simultâneas
        /// </summary>
        public async Task<StressTestResult> ExecutarTesteBurstAsync(
            int leiturasPorBurst = 20,
            int numeroBursts = 5,
            int intervaloEntreBurstsMs = 2000,
            int idEtapa = 1)
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();

            var totalLeituras = leiturasPorBurst * numeroBursts;
            var resultado = new StressTestResult
            {
                NomeTeste = "Burst (Rajada)",
                TotalEsperado = totalLeituras
            };

            var stopwatch = Stopwatch.StartNew();
            var leiturasProcessadas = 0;
            var erros = new List<string>();
            var temposBurst = new List<double>();

            try
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StressTest] Burst: {numeroBursts} rajadas de {leiturasPorBurst} leituras");

                for (int burst = 1; burst <= numeroBursts && !_cts.Token.IsCancellationRequested; burst++)
                {
                    var swBurst = Stopwatch.StartNew();

                    // Criar todas as leituras do burst simultaneamente
                    var tasks = new List<Task<bool>>();

                    for (int i = 1; i <= leiturasPorBurst; i++)
                    {
                        var numeroMoto = ((burst - 1) * leiturasPorBurst + i) % 99 + 1;
                        tasks.Add(ProcessarLeituraAsync(numeroMoto, idEtapa));
                    }

                    // Aguardar todas
                    var resultados = await Task.WhenAll(tasks);

                    swBurst.Stop();
                    temposBurst.Add(swBurst.Elapsed.TotalMilliseconds);

                    leiturasProcessadas += resultados.Count(r => r);
                    var errosBurst = resultados.Count(r => !r);

                    if (errosBurst > 0)
                    {
                        erros.Add($"Burst {burst}: {errosBurst} erros");
                    }

                    ProgressChanged?.Invoke(this, new StressTestProgressEventArgs
                    {
                        Atual = burst * leiturasPorBurst,
                        Total = totalLeituras,
                        Percentual = (double)burst / numeroBursts * 100,
                        Mensagem = $"Burst {burst}/{numeroBursts} em {swBurst.ElapsedMilliseconds}ms"
                    });

                    System.Diagnostics.Debug.WriteLine(
                        $"[StressTest] Burst {burst}: {leiturasPorBurst - errosBurst}/{leiturasPorBurst} " +
                        $"em {swBurst.ElapsedMilliseconds}ms");

                    // Intervalo entre bursts
                    if (burst < numeroBursts)
                    {
                        await Task.Delay(intervaloEntreBurstsMs, _cts.Token);
                    }
                }

                stopwatch.Stop();

                resultado.TotalProcessado = leiturasProcessadas;
                resultado.TotalErros = totalLeituras - leiturasProcessadas;
                resultado.TempoTotalMs = stopwatch.ElapsedMilliseconds;
                resultado.Erros = erros;

                if (temposBurst.Count > 0)
                {
                    resultado.TempoMedioProcessamentoMs = temposBurst.Average();
                    resultado.TempoMaximoProcessamentoMs = temposBurst.Max();
                }

                resultado.LeiturasSegundo = leiturasProcessadas / (stopwatch.ElapsedMilliseconds / 1000.0);
                resultado.Sucesso = leiturasProcessadas == totalLeituras;
            }
            catch (OperationCanceledException)
            {
                resultado.Cancelado = true;
            }
            finally
            {
                _isRunning = false;
                TestCompleted?.Invoke(this, new StressTestResultEventArgs { Resultado = resultado });
            }

            return resultado;
        }

        private async Task<bool> ProcessarLeituraAsync(int numeroMoto, int idEtapa)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var dados = $"{numeroMoto},{timestamp:yyyy-MM-dd HH:mm:ss.fff}";

                var parseResult = _parserService.Parsear(dados, "GENERICO");
                if (!parseResult.Sucesso) return false;

                var leitura = parseResult.Leitura!;
                leitura.IdEtapa = idEtapa;
                leitura.DeviceId = "BURST-TEST";
                leitura.DadosBrutos = dados;
                leitura.GerarHash();

                await _storageService.SalvarLeituraAsync(leitura);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Teste de memória - verificar vazamentos
        /// </summary>
        public async Task<MemoryTestResult> ExecutarTesteMemoriaAsync(int ciclos = 10, int leiturasPorCiclo = 100)
        {
            var resultado = new MemoryTestResult();
            var memoriaInicial = GC.GetTotalMemory(true);

            resultado.MemoriaInicialBytes = memoriaInicial;

            try
            {
                for (int ciclo = 1; ciclo <= ciclos; ciclo++)
                {
                    // Executar leituras
                    await ExecutarTesteLeiturasRapidasAsync(leiturasPorCiclo, 10, 1);

                    // Forçar coleta de lixo
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    var memoriaAtual = GC.GetTotalMemory(false);
                    resultado.MemoriaPorCiclo.Add(memoriaAtual);

                    System.Diagnostics.Debug.WriteLine(
                        $"[MemoryTest] Ciclo {ciclo}: {memoriaAtual / 1024}KB " +
                        $"(Delta: {(memoriaAtual - memoriaInicial) / 1024}KB)");

                    ProgressChanged?.Invoke(this, new StressTestProgressEventArgs
                    {
                        Atual = ciclo,
                        Total = ciclos,
                        Percentual = (double)ciclo / ciclos * 100,
                        Mensagem = $"Memória: {memoriaAtual / 1024}KB"
                    });
                }

                resultado.MemoriaFinalBytes = GC.GetTotalMemory(true);
                resultado.VazamentoDetectado = (resultado.MemoriaFinalBytes - memoriaInicial) > 10 * 1024 * 1024; // >10MB
                resultado.Sucesso = !resultado.VazamentoDetectado;
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Erro = ex.Message;
            }

            return resultado;
        }

        public void Cancelar()
        {
            _cts?.Cancel();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLASSES DE RESULTADO
    // ═══════════════════════════════════════════════════════════════════════

    public class StressTestResult
    {
        public string NomeTeste { get; set; } = "";
        public bool Sucesso { get; set; }
        public bool Cancelado { get; set; }
        public int TotalEsperado { get; set; }
        public int TotalProcessado { get; set; }
        public int TotalErros { get; set; }
        public int IntervaloMs { get; set; }
        public long TempoTotalMs { get; set; }
        public double TempoMedioProcessamentoMs { get; set; }
        public double TempoMaximoProcessamentoMs { get; set; }
        public double TempoMinimoProcessamentoMs { get; set; }
        public double LeiturasSegundo { get; set; }
        public List<string> Erros { get; set; } = new();

        public string Resumo => $"""
            Teste: {NomeTeste}
            Status: {(Sucesso ? "✅ PASSOU" : "❌ FALHOU")}
            Processadas: {TotalProcessado}/{TotalEsperado}
            Erros: {TotalErros}
            Tempo Total: {TempoTotalMs}ms
            Taxa: {LeiturasSegundo:F1} leituras/segundo
            Tempo Médio: {TempoMedioProcessamentoMs:F2}ms
            Tempo Máximo: {TempoMaximoProcessamentoMs:F2}ms
            """;
    }

    public class MemoryTestResult
    {
        public bool Sucesso { get; set; }
        public long MemoriaInicialBytes { get; set; }
        public long MemoriaFinalBytes { get; set; }
        public List<long> MemoriaPorCiclo { get; set; } = new();
        public bool VazamentoDetectado { get; set; }
        public string? Erro { get; set; }

        public long DeltaBytes => MemoriaFinalBytes - MemoriaInicialBytes;
    }

    public class StressTestProgressEventArgs : EventArgs
    {
        public int Atual { get; set; }
        public int Total { get; set; }
        public double Percentual { get; set; }
        public string? Mensagem { get; set; }
    }

    public class StressTestResultEventArgs : EventArgs
    {
        public StressTestResult Resultado { get; set; } = new();
    }
}