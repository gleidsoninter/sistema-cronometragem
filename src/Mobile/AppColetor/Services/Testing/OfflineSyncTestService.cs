using AppColetor.Models.Entities;
using AppColetor.Services.Implementations;
using AppColetor.Services.Interfaces;
using System.Diagnostics;

namespace AppColetor.Services.Testing
{
    public class OfflineSyncTestService
    {
        private readonly IStorageService _storageService;
        private readonly IApiService _apiService;
        private readonly SyncService _syncService;
        private readonly IConnectivityService _connectivityService;

        public event EventHandler<OfflineSyncTestEventArgs>? ProgressChanged;

        public OfflineSyncTestService(
            IStorageService storageService,
            IApiService apiService,
            SyncService syncService,
            IConnectivityService connectivityService)
        {
            _storageService = storageService;
            _apiService = apiService;
            _syncService = syncService;
            _connectivityService = connectivityService;
        }

        /// <summary>
        /// Testa o fluxo completo de sincronização offline
        /// </summary>
        public async Task<OfflineSyncTestResult> ExecutarTesteAsync(
            int leiturasParaCriar = 50,
            int idEtapa = 1)
        {
            var resultado = new OfflineSyncTestResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                System.Diagnostics.Debug.WriteLine("[OfflineSyncTest] Iniciando teste...");

                // FASE 1: Contar leituras pendentes existentes
                ReportarProgresso("Verificando estado inicial...", 1, 5);

                var pendentesAntes = await _storageService.ContarLeiturasNaoSincronizadasAsync();
                resultado.PendentesAntes = pendentesAntes;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineSyncTest] Pendentes antes: {pendentesAntes}");

                // FASE 2: Criar leituras "offline" (simulando)
                ReportarProgresso($"Criando {leiturasParaCriar} leituras offline...", 2, 5);

                var swCriacao = Stopwatch.StartNew();
                var leiturasCriadas = new List<Leitura>();

                for (int i = 1; i <= leiturasParaCriar; i++)
                {
                    var leitura = new Leitura
                    {
                        NumeroMoto = (i % 99) + 1,
                        Timestamp = DateTime.UtcNow,
                        Tipo = "P",
                        IdEtapa = idEtapa,
                        DeviceId = "OFFLINE-TEST",
                        DadosBrutos = $"OFFLINE_TEST_{i}",
                        Sincronizado = false
                    };
                    leitura.GerarHash();

                    var id = await _storageService.SalvarLeituraAsync(leitura);
                    leitura.Id = id;
                    leiturasCriadas.Add(leitura);
                }

                swCriacao.Stop();
                resultado.TempoCriacaoMs = swCriacao.ElapsedMilliseconds;
                resultado.LeiturasACriar = leiturasParaCriar;
                resultado.LeiturasCriadas = leiturasCriadas.Count;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineSyncTest] Criadas {leiturasCriadas.Count} leituras em {swCriacao.ElapsedMilliseconds}ms");

                // FASE 3: Verificar que estão pendentes
                ReportarProgresso("Verificando armazenamento local...", 3, 5);

                var pendentesAposCriacao = await _storageService.ContarLeiturasNaoSincronizadasAsync();
                resultado.PendentesAposCriacao = pendentesAposCriacao;

                var armazenamentoOk = pendentesAposCriacao >= pendentesAntes + leiturasParaCriar;
                resultado.ArmazenamentoLocalOk = armazenamentoOk;

                if (!armazenamentoOk)
                {
                    resultado.Sucesso = false;
                    resultado.Erro = "Falha no armazenamento local: contagem incorreta";
                    return resultado;
                }

                // FASE 4: Executar sincronização
                ReportarProgresso("Executando sincronização...", 4, 5);

                var swSync = Stopwatch.StartNew();

                // Verificar se está online
                if (!_connectivityService.IsOnline)
                {
                    resultado.Online = false;
                    resultado.Sucesso = true;
                    resultado.Mensagem = "Dispositivo offline - leituras armazenadas para sync posterior";
                    return resultado;
                }

                resultado.Online = true;

                // Executar sync
                await _syncService.ForcarSyncAsync();

                swSync.Stop();
                resultado.TempoSyncMs = swSync.ElapsedMilliseconds;

                // FASE 5: Verificar resultado
                ReportarProgresso("Verificando resultado...", 5, 5);

                await Task.Delay(1000); // Aguardar processamento

                var pendentesAposSync = await _storageService.ContarLeiturasNaoSincronizadasAsync();
                resultado.PendentesAposSync = pendentesAposSync;
                resultado.LeiturasSincronizadas = pendentesAposCriacao - pendentesAposSync;

                // Calcular taxa de sucesso
                resultado.TaxaSucesso = leiturasParaCriar > 0
                    ? (double)resultado.LeiturasSincronizadas / leiturasParaCriar * 100
                    : 100;

                resultado.Sucesso = resultado.LeiturasSincronizadas >= leiturasParaCriar * 0.95; // 95%+ sucesso

                stopwatch.Stop();
                resultado.TempoTotalMs = stopwatch.ElapsedMilliseconds;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineSyncTest] Concluído: {resultado.LeiturasSincronizadas}/{leiturasParaCriar} " +
                    $"sincronizadas ({resultado.TaxaSucesso:F1}%)");
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Erro = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[OfflineSyncTest] Erro: {ex.Message}");
            }

            return resultado;
        }

        /// <summary>
        /// Testa integridade dos dados após sync
        /// </summary>
        public async Task<IntegrityTestResult> TestarIntegridadeDadosAsync()
        {
            var resultado = new IntegrityTestResult();

            try
            {
                // Buscar leituras sincronizadas
                var leituras = await _storageService.GetLeiturasRecentesAsync(100);

                foreach (var leitura in leituras)
                {
                    // Verificar campos obrigatórios
                    if (leitura.NumeroMoto <= 0)
                        resultado.Erros.Add($"Leitura {leitura.Id}: NumeroMoto inválido");

                    if (leitura.Timestamp == default)
                        resultado.Erros.Add($"Leitura {leitura.Id}: Timestamp inválido");

                    if (string.IsNullOrEmpty(leitura.Hash))
                        resultado.Erros.Add($"Leitura {leitura.Id}: Hash vazio");

                    if (leitura.IdEtapa <= 0)
                        resultado.Erros.Add($"Leitura {leitura.Id}: IdEtapa inválido");

                    // Verificar consistência do hash
                    var hashOriginal = leitura.Hash;
                    leitura.GerarHash();

                    if (hashOriginal != leitura.Hash)
                    {
                        resultado.HashInconsistentes++;
                    }
                }

                resultado.TotalVerificadas = leituras.Count;
                resultado.Sucesso = resultado.Erros.Count == 0 && resultado.HashInconsistentes == 0;
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.Erros.Add($"Exceção: {ex.Message}");
            }

            return resultado;
        }

        private void ReportarProgresso(string mensagem, int atual, int total)
        {
            ProgressChanged?.Invoke(this, new OfflineSyncTestEventArgs
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

    public class OfflineSyncTestResult
    {
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public string? Mensagem { get; set; }
        public bool Online { get; set; }
        public bool ArmazenamentoLocalOk { get; set; }
        public int LeiturasACriar { get; set; }
        public int LeiturasCriadas { get; set; }
        public int LeiturasSincronizadas { get; set; }
        public int PendentesAntes { get; set; }
        public int PendentesAposCriacao { get; set; }
        public int PendentesAposSync { get; set; }
        public double TaxaSucesso { get; set; }
        public long TempoCriacaoMs { get; set; }
        public long TempoSyncMs { get; set; }
        public long TempoTotalMs { get; set; }

        public string Resumo => $"""
            Teste de Sincronização Offline
            Status: {(Sucesso ? "✅ PASSOU" : "❌ FALHOU")}
            Online: {(Online ? "Sim" : "Não")}
            Armazenamento Local: {(ArmazenamentoLocalOk ? "OK" : "FALHOU")}
            Leituras Criadas: {LeiturasCriadas}/{LeiturasACriar}
            Sincronizadas: {LeiturasSincronizadas}
            Taxa de Sucesso: {TaxaSucesso:F1}%
            Tempo Sync: {TempoSyncMs}ms
            {(Erro != null ? $"Erro: {Erro}" : "")}
            """;
    }

    public class IntegrityTestResult
    {
        public bool Sucesso { get; set; }
        public int TotalVerificadas { get; set; }
        public int HashInconsistentes { get; set; }
        public List<string> Erros { get; set; } = new();
    }

    public class OfflineSyncTestEventArgs : EventArgs
    {
        public string Mensagem { get; set; } = "";
        public int Atual { get; set; }
        public int Total { get; set; }
    }
}