using AppColetor.Data;
using AppColetor.Helpers;
using AppColetor.Models.DTOs;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class SyncService : IDisposable
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════════════════════════════

        private const int BATCH_SIZE_WIFI = 50;
        private const int BATCH_SIZE_CELLULAR = 20;
        private const int SYNC_INTERVAL_WIFI_MS = 5000;      // 5 segundos
        private const int SYNC_INTERVAL_CELLULAR_MS = 15000; // 15 segundos
        private const int HEARTBEAT_INTERVAL_MS = 30000;     // 30 segundos

        // ═══════════════════════════════════════════════════════════════════
        // DEPENDÊNCIAS
        // ═══════════════════════════════════════════════════════════════════

        private readonly IApiService _apiService;
        private readonly IQueueService _queueService;
        private readonly IConnectivityService _connectivityService;
        private readonly IStorageService _storageService;
        private readonly IConfigService _configService;
        private readonly AppDatabase _database;
        private readonly BatteryOptimizationService _batteryService;

        // ═══════════════════════════════════════════════════════════════════
        // ESTADO
        // ═══════════════════════════════════════════════════════════════════

        private Timer? _syncTimer;
        private Timer? _heartbeatTimer;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _isSyncing;
        private bool _disposed;
        private DateTime _ultimoSync = DateTime.MinValue;

        // ═══════════════════════════════════════════════════════════════════
        // EVENTOS
        // ═══════════════════════════════════════════════════════════════════

        public event EventHandler<SyncProgressEventArgs>? SyncProgress;
        public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
        public event EventHandler<SyncErrorEventArgs>? SyncError;

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES
        // ═══════════════════════════════════════════════════════════════════

        public bool IsRunning => _isRunning;
        public bool IsSyncing => _isSyncing;
        public DateTime UltimoSync => _ultimoSync;

        // ═══════════════════════════════════════════════════════════════════
        // CONSTRUTOR
        // ═══════════════════════════════════════════════════════════════════

        public SyncService(
            IApiService apiService,
            IQueueService queueService,
            IConnectivityService connectivityService,
            IStorageService storageService,
            IConfigService configService,
            AppDatabase database,
            BatteryOptimizationService batteryService)
        {
            _apiService = apiService;
            _queueService = queueService;
            _connectivityService = connectivityService;
            _storageService = storageService;
            _configService = configService;
            _database = database;

            // Inscrever no evento de mudança de conectividade
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;
            _batteryService = batteryService;
            _batteryService.ModeChanged += OnBatteryModeChanged;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONTROLE DO SERVIÇO
        // ═══════════════════════════════════════════════════════════════════

        public void Iniciar()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            // Iniciar timer com intervalo baseado no tipo de conexão
            AtualizarTimerSync();

            // Timer de heartbeat
            _heartbeatTimer = new Timer(
                async _ => await EnviarHeartbeatAsync(),
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(HEARTBEAT_INTERVAL_MS));

            System.Diagnostics.Debug.WriteLine("[Sync] Serviço de sincronização iniciado");
        }

        private void OnBatteryModeChanged(object? sender, BatteryModeChangedEventArgs e)
        {
            // Reconfigurar intervalos baseado no modo de bateria
            var settings = _batteryService.ObterConfiguracoes();

            // Atualizar timer de sync
            _syncTimer?.Change(
                TimeSpan.FromMilliseconds(settings.IntervaloSyncMs),
                TimeSpan.FromMilliseconds(settings.IntervaloSyncMs));

            // Atualizar timer de heartbeat
            _heartbeatTimer?.Change(
                TimeSpan.FromMilliseconds(settings.IntervaloHeartbeatMs),
                TimeSpan.FromMilliseconds(settings.IntervaloHeartbeatMs));

            System.Diagnostics.Debug.WriteLine(
                $"[Sync] Intervalos ajustados para modo {e.ModoAtual}: " +
                $"Sync={settings.IntervaloSyncMs}ms, Heartbeat={settings.IntervaloHeartbeatMs}ms");
        }
        public void Parar()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cts?.Cancel();

            _syncTimer?.Dispose();
            _syncTimer = null;

            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;

            System.Diagnostics.Debug.WriteLine("[Sync] Serviço de sincronização parado");
        }

        private void AtualizarTimerSync()
        {
            _syncTimer?.Dispose();

            var intervalo = _connectivityService.IsWiFi
                ? SYNC_INTERVAL_WIFI_MS
                : SYNC_INTERVAL_CELLULAR_MS;

            _syncTimer = new Timer(
                async _ => await ExecutarSyncAsync(),
                null,
                TimeSpan.FromMilliseconds(intervalo),
                TimeSpan.FromMilliseconds(intervalo));

            System.Diagnostics.Debug.WriteLine(
                $"[Sync] Timer configurado: {intervalo}ms ({(_connectivityService.IsWiFi ? "WiFi" : "Cellular")})");
        }

        // ═══════════════════════════════════════════════════════════════════
        // EVENTOS DE CONECTIVIDADE
        // ═══════════════════════════════════════════════════════════════════

        private async void OnConnectivityChanged(object? sender, ConnectivityEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Sync] Conectividade mudou: {e.TipoAnterior} → {e.TipoAtual}, Online: {e.IsOnline}");

            if (e.IsOnline && !_isSyncing)
            {
                // Reconectou - sincronizar imediatamente
                System.Diagnostics.Debug.WriteLine("[Sync] Reconectado! Iniciando sincronização...");

                // Atualizar intervalo do timer
                AtualizarTimerSync();

                // Executar sync imediato
                await ExecutarSyncAsync();
            }
            else if (!e.IsOnline)
            {
                System.Diagnostics.Debug.WriteLine("[Sync] Offline. Leituras serão armazenadas localmente.");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // SINCRONIZAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        public async Task ExecutarSyncAsync()
        {
            if (_isSyncing || !_isRunning)
                return;

            if (!_connectivityService.IsOnline)
            {
                System.Diagnostics.Debug.WriteLine("[Sync] Offline - sync cancelado");
                return;
            }

            _isSyncing = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("[Sync] Iniciando sincronização...");

                // Obter leituras não sincronizadas
                var pendentes = await _storageService.GetLeiturasNaoSincronizadasAsync();

                if (pendentes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[Sync] Nenhuma leitura pendente");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[Sync] {pendentes.Count} leituras pendentes");

                // Determinar tamanho do lote baseado na conexão
                var batchSize = _connectivityService.IsWiFi
                    ? BATCH_SIZE_WIFI
                    : BATCH_SIZE_CELLULAR;

                // Dividir em lotes
                var lotes = DividirEmLotes(pendentes, batchSize);
                var totalLotes = lotes.Count;
                var loteAtual = 0;
                var totalSincronizadas = 0;
                var totalErros = 0;

                foreach (var lote in lotes)
                {
                    if (_cts?.Token.IsCancellationRequested == true)
                        break;

                    if (!_connectivityService.IsOnline)
                    {
                        System.Diagnostics.Debug.WriteLine("[Sync] Conexão perdida durante sync");
                        break;
                    }

                    loteAtual++;

                    // Reportar progresso
                    SyncProgress?.Invoke(this, new SyncProgressEventArgs
                    {
                        LoteAtual = loteAtual,
                        TotalLotes = totalLotes,
                        LeiturasProcessadas = totalSincronizadas,
                        TotalLeituras = pendentes.Count
                    });

                    // Enviar lote
                    var resultado = await EnviarLoteAsync(lote);

                    if (resultado.Sucesso)
                    {
                        totalSincronizadas += resultado.Sincronizadas;

                        // Marcar como sincronizadas
                        var ids = resultado.LeiturasSincronizadas.Select(l => l.Id);
                        await _database.MarcarLoteSincronizadoAsync(ids);
                    }
                    else
                    {
                        totalErros += lote.Count;
                    }

                    // Pequeno delay entre lotes
                    if (loteAtual < totalLotes && _connectivityService.IsCellular)
                    {
                        await Task.Delay(500, _cts?.Token ?? default);
                    }
                }

                _ultimoSync = DateTime.UtcNow;

                System.Diagnostics.Debug.WriteLine(
                    $"[Sync] Concluído: {totalSincronizadas} sincronizadas, {totalErros} erros");

                // Reportar conclusão
                SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
                {
                    TotalSincronizadas = totalSincronizadas,
                    TotalErros = totalErros,
                    DataHora = _ultimoSync
                });
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[Sync] Sincronização cancelada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Erro: {ex.Message}");

                SyncError?.Invoke(this, new SyncErrorEventArgs
                {
                    Mensagem = ex.Message,
                    Exception = ex
                });
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private async Task<LoteResultado> EnviarLoteAsync(List<Leitura> lote)
        {
            var resultado = new LoteResultado();

            try
            {
                var response = await _apiService.EnviarLoteAsync(lote, _cts?.Token ?? default);

                if (response == null)
                {
                    resultado.Sucesso = false;
                    return resultado;
                }

                resultado.Sucesso = true;
                resultado.Sincronizadas = response.TotalProcessadas + response.TotalDuplicadas;

                // Marcar leituras sincronizadas
                if (response.Detalhes != null)
                {
                    foreach (var detalhe in response.Detalhes)
                    {
                        if (detalhe.Status == "OK" || detalhe.Status == "DUPLICADA")
                        {
                            var leitura = lote.FirstOrDefault(l => l.NumeroMoto == detalhe.NumeroMoto);
                            if (leitura != null)
                            {
                                resultado.LeiturasSincronizadas.Add(leitura);
                            }
                        }
                    }
                }
                else
                {
                    // Se não veio detalhes, assumir que todas foram sincronizadas
                    resultado.LeiturasSincronizadas.AddRange(lote);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Erro no lote: {ex.Message}");
                resultado.Sucesso = false;
                resultado.Erro = ex.Message;
            }

            return resultado;
        }

        private List<List<T>> DividirEmLotes<T>(List<T> lista, int tamanhoLote)
        {
            var lotes = new List<List<T>>();

            for (int i = 0; i < lista.Count; i += tamanhoLote)
            {
                lotes.Add(lista.GetRange(i, Math.Min(tamanhoLote, lista.Count - i)));
            }

            return lotes;
        }

        // ═══════════════════════════════════════════════════════════════════
        // HEARTBEAT
        // ═══════════════════════════════════════════════════════════════════

        private async Task EnviarHeartbeatAsync()
        {
            if (!_connectivityService.IsOnline)
                return;

            try
            {
                var deviceId = await _configService.GetStringAsync(Constants.KEY_DEVICE_ID);

                if (!string.IsNullOrEmpty(deviceId))
                {
                    await _apiService.EnviarHeartbeatAsync(deviceId, _cts?.Token ?? default);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Erro no heartbeat: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // FORÇA SYNC
        // ═══════════════════════════════════════════════════════════════════

        public async Task ForcarSyncAsync()
        {
            if (!_connectivityService.IsOnline)
            {
                throw new InvalidOperationException("Sem conexão com a internet");
            }

            await ExecutarSyncAsync();
        }

        // ═══════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
            Parar();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLASSES AUXILIARES
    // ═══════════════════════════════════════════════════════════════════════

    public class LoteResultado
    {
        public bool Sucesso { get; set; }
        public int Sincronizadas { get; set; }
        public string? Erro { get; set; }
        public List<Leitura> LeiturasSincronizadas { get; set; } = new();
    }

    public class SyncProgressEventArgs : EventArgs
    {
        public int LoteAtual { get; set; }
        public int TotalLotes { get; set; }
        public int LeiturasProcessadas { get; set; }
        public int TotalLeituras { get; set; }
        public double Percentual => TotalLeituras > 0
            ? (double)LeiturasProcessadas / TotalLeituras * 100
            : 0;
    }

    public class SyncCompletedEventArgs : EventArgs
    {
        public int TotalSincronizadas { get; set; }
        public int TotalErros { get; set; }
        public DateTime DataHora { get; set; }
    }

    public class SyncErrorEventArgs : EventArgs
    {
        public string Mensagem { get; set; } = "";
        public Exception? Exception { get; set; }
    }
}