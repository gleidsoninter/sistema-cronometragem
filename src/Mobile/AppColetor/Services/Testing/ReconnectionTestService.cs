using AppColetor.Services.Implementations;
using AppColetor.Services.Interfaces;
using System.Diagnostics;

namespace AppColetor.Services.Testing
{
    public class ReconnectionTestService
    {
        private readonly IConnectivityService _connectivityService;
        private readonly IApiService _apiService;
        private readonly SyncService _syncService;

        public event EventHandler<ReconnectionTestEventArgs>? ProgressChanged;

        public ReconnectionTestService(
            IConnectivityService connectivityService,
            IApiService apiService,
            SyncService syncService)
        {
            _connectivityService = connectivityService;
            _apiService = apiService;
            _syncService = syncService;
        }

        /// <summary>
        /// Testa comportamento de reconexão
        /// </summary>
        public async Task<ReconnectionTestResult> ExecutarTesteAsync(
            int ciclosSimulacao = 5,
            int tempoOfflineMs = 5000,
            int tempoOnlineMs = 10000)
        {
            var resultado = new ReconnectionTestResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                System.Diagnostics.Debug.WriteLine("[ReconnTest] Iniciando teste de reconexão...");

                resultado.StatusInicial = _connectivityService.IsOnline;
                resultado.TipoConexaoInicial = _connectivityService.TipoAtual.ToString();

                for (int ciclo = 1; ciclo <= ciclosSimulacao; ciclo++)
                {
                    var cicloDados = new CicloReconexao { Numero = ciclo };
                    var swCiclo = Stopwatch.StartNew();

                    // FASE 1: Registrar estado online
                    cicloDados.OnlineAntes = _connectivityService.IsOnline;
                    cicloDados.ApiConectadaAntes = _apiService.IsConnected;

                    ReportarProgresso($"Ciclo {ciclo}: Simulando período online...", ciclo, ciclosSimulacao * 2);

                    // Verificar API
                    var verificacaoApi = await _apiService.VerificarConexaoAsync();
                    cicloDados.VerificacaoApiSucesso = verificacaoApi;

                    await Task.Delay(tempoOnlineMs / 2);

                    // FASE 2: Simular offline (não podemos realmente desconectar)
                    // Mas podemos testar o comportamento do sistema
                    ReportarProgresso($"Ciclo {ciclo}: Testando detecção de falha...", ciclo * 2 - 1, ciclosSimulacao * 2);

                    var swDeteccao = Stopwatch.StartNew();

                    // Simular timeout de API
                    var apiTimeout = await TestarTimeoutApiAsync();
                    cicloDados.TempoDeteccaoFalhaMs = swDeteccao.ElapsedMilliseconds;
                    cicloDados.TimeoutApiDetectado = apiTimeout;

                    await Task.Delay(tempoOfflineMs);

                    // FASE 3: Verificar reconexão
                    ReportarProgresso($"Ciclo {ciclo}: Verificando reconexão...", ciclo * 2, ciclosSimulacao * 2);

                    var swReconexao = Stopwatch.StartNew();

                    // Verificar se reconectou
                    var reconectou = await _apiService.VerificarConexaoAsync();

                    swReconexao.Stop();
                    cicloDados.TempoReconexaoMs = swReconexao.ElapsedMilliseconds;
                    cicloDados.ReconexaoSucesso = reconectou;

                    // FASE 4: Verificar sync após reconexão
                    if (reconectou)
                    {
                        var swSync = Stopwatch.StartNew();
                        await _syncService.ExecutarSyncAsync();
                        swSync.Stop();
                        cicloDados.TempoSyncAposReconexaoMs = swSync.ElapsedMilliseconds;
                    }

                    swCiclo.Stop();
                    cicloDados.TempoTotalCicloMs = swCiclo.ElapsedMilliseconds;

                    resultado.Ciclos.Add(cicloDados);

                    System.Diagnostics.Debug.WriteLine(
                        $"[ReconnTest] Ciclo {ciclo}: Reconexão={reconectou}, " +
                        $"Tempo={cicloDados.TempoReconexaoMs}ms");
                }

                stopwatch.Stop();

                // Calcular métricas
                resultado.TempoTotalMs = stopwatch.ElapsedMilliseconds;
                resultado.TotalCiclos = ciclosSimulacao;
                resultado.CiclosSucesso = resultado.Ciclos.Count(c => c.ReconexaoSucesso);
                resultado.TempoMedioReconexaoMs = resultado.Ciclos.Average(c => c.TempoReconexaoMs);
                resultado.Sucesso = resultado.CiclosSucesso == ciclosSimulacao;

                System.Diagnostics.Debug.WriteLine(
                    $"[ReconnTest] Concluído: {resultado.CiclosSucesso}/{ciclosSimulacao} reconexões bem-sucedidas");
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Erro = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ReconnTest] Erro: {ex.Message}");
            }

            return resultado;
        }

        private async Task<bool> TestarTimeoutApiAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _apiService.VerificarConexaoAsync(cts.Token);
                return false; // Não houve timeout
            }
            catch (OperationCanceledException)
            {
                return true; // Timeout detectado
            }
            catch
            {
                return true;
            }
        }

        private void ReportarProgresso(string mensagem, int atual, int total)
        {
            ProgressChanged?.Invoke(this, new ReconnectionTestEventArgs
            {
                Mensagem = mensagem,
                Atual = atual,
                Total = total
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLASSES DE RESULTADO
    // ═══════════════════════════════════════════════════════════════════════

    public class ReconnectionTestResult
    {
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public bool StatusInicial { get; set; }
        public string TipoConexaoInicial { get; set; } = "";
        public int TotalCiclos { get; set; }
        public int CiclosSucesso { get; set; }
        public long TempoTotalMs { get; set; }
        public double TempoMedioReconexaoMs { get; set; }
        public List<CicloReconexao> Ciclos { get; set; } = new();

        public string Resumo => $"""
            Teste de Reconexão
            Status: {(Sucesso ? "✅ PASSOU" : "❌ FALHOU")}
            Ciclos: {CiclosSucesso}/{TotalCiclos} bem-sucedidos
            Tempo Médio Reconexão: {TempoMedioReconexaoMs:F0}ms
            Tempo Total: {TempoTotalMs}ms
            """;
    }

    public class CicloReconexao
    {
        public int Numero { get; set; }
        public bool OnlineAntes { get; set; }
        public bool ApiConectadaAntes { get; set; }
        public bool VerificacaoApiSucesso { get; set; }
        public bool TimeoutApiDetectado { get; set; }
        public long TempoDeteccaoFalhaMs { get; set; }
        public bool ReconexaoSucesso { get; set; }
        public long TempoReconexaoMs { get; set; }
        public long TempoSyncAposReconexaoMs { get; set; }
        public long TempoTotalCicloMs { get; set; }
    }

    public class ReconnectionTestEventArgs : EventArgs
    {
        public string Mensagem { get; set; } = "";
        public int Atual { get; set; }
        public int Total { get; set; }
    }
}