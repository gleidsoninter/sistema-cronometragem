using AppColetor.Helpers;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    /// <summary>
    /// Serviço de sincronização em background
    /// </summary>
    public class SyncBackgroundService : IDisposable
    {
        private readonly IApiService _apiService;
        private readonly IStorageService _storageService;
        private readonly IConfigService _configService;

        private Timer? _syncTimer;
        private Timer? _heartbeatTimer;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _isSyncing;
        private bool _disposed;

        private const int DEFAULT_SYNC_INTERVAL_SECONDS = 5;
        private const int HEARTBEAT_INTERVAL_SECONDS = 30;

        public event EventHandler<SyncEventArgs>? SyncCompleted;
        public event EventHandler<SyncEventArgs>? SyncFailed;

        public bool IsRunning => _isRunning;
        public bool IsSyncing => _isSyncing;

        public SyncBackgroundService(
            IApiService apiService,
            IStorageService storageService,
            IConfigService configService)
        {
            _apiService = apiService;
            _storageService = storageService;
            _configService = configService;
        }

        /// <summary>
        /// Inicia o serviço de sincronização
        /// </summary>
        public void Iniciar()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            // Timer de sincronização
            var intervalo = Preferences.Get("intervalo_sync", DEFAULT_SYNC_INTERVAL_SECONDS);
            _syncTimer = new Timer(
                OnSyncTimerElapsed,
                null,
                TimeSpan.FromSeconds(intervalo),
                TimeSpan.FromSeconds(intervalo));

            // Timer de heartbeat
            _heartbeatTimer = new Timer(
                OnHeartbeatTimerElapsed,
                null,
                TimeSpan.FromSeconds(HEARTBEAT_INTERVAL_SECONDS),
                TimeSpan.FromSeconds(HEARTBEAT_INTERVAL_SECONDS));

            System.Diagnostics.Debug.WriteLine($"[Sync] Serviço iniciado. Intervalo: {intervalo}s");
        }

        /// <summary>
        /// Para o serviço de sincronização
        /// </summary>
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

            System.Diagnostics.Debug.WriteLine("[Sync] Serviço parado");
        }

        private async void OnSyncTimerElapsed(object? state)
        {
            if (!_isRunning || _isSyncing)
                return;

            // Verificar se sync automático está habilitado
            if (!Preferences.Get("sync_automatico", true))
                return;

            // Verificar conectividade
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;

            // Verificar se API está conectada
            if (!_apiService.IsConnected)
            {
                // Tentar verificar conexão
                await _apiService.VerificarConexaoAsync(_cts?.Token ?? default);

                if (!_apiService.IsConnected)
                    return;
            }

            await ExecutarSincronizacaoAsync();
        }

        /// <summary>
        /// Executa sincronização das leituras pendentes
        /// </summary>
        public async Task ExecutarSincronizacaoAsync()
        {
            if (_isSyncing)
                return;

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

                // Sincronizar
                var sincronizadas = await _apiService.SincronizarLeiturasAsync(
                    pendentes,
                    _cts?.Token ?? default);

                // Marcar como sincronizadas
                foreach (var leitura in sincronizadas)
                {
                    await _storageService.MarcarComoSincronizadaAsync(leitura.Id);
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[Sync] Sincronização concluída: {sincronizadas.Count}/{pendentes.Count}");

                // Disparar evento
                SyncCompleted?.Invoke(this, new SyncEventArgs
                {
                    TotalPendentes = pendentes.Count,
                    TotalSincronizadas = sincronizadas.Count,
                    Sucesso = true
                });
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[Sync] Sincronização cancelada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Sync] Erro na sincronização: {ex.Message}");

                SyncFailed?.Invoke(this, new SyncEventArgs
                {
                    Sucesso = false,
                    Erro = ex.Message
                });
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private async void OnHeartbeatTimerElapsed(object? state)
        {
            if (!_isRunning)
                return;

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
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

        /// <summary>
        /// Força uma sincronização imediata
        /// </summary>
        public async Task ForcarSincronizacaoAsync()
        {
            await ExecutarSincronizacaoAsync();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Parar();
        }
    }

    public class SyncEventArgs : EventArgs
    {
        public int TotalPendentes { get; set; }
        public int TotalSincronizadas { get; set; }
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
    }
}