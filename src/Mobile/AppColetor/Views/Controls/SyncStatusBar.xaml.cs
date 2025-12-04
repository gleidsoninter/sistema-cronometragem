using AppColetor.Services.Interfaces;
using AppColetor.Services.Implementations;

namespace AppColetor.Views.Controls
{
    public partial class SyncStatusBar : ContentView
    {
        private IConnectivityService? _connectivityService;
        private SyncService? _syncService;
        private IStorageService? _storageService;
        private Timer? _updateTimer;

        public SyncStatusBar()
        {
            InitializeComponent();
        }

        public void Inicializar(
            IConnectivityService connectivityService,
            SyncService syncService,
            IStorageService storageService)
        {
            _connectivityService = connectivityService;
            _syncService = syncService;
            _storageService = storageService;

            // Inscrever em eventos
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;
            _syncService.SyncProgress += OnSyncProgress;
            _syncService.SyncCompleted += OnSyncCompleted;

            // Atualizar estado inicial
            AtualizarEstadoConexao();

            // Timer para atualizar contador de pendentes
            _updateTimer = new Timer(
                async _ => await AtualizarContadorPendentesAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5));
        }

        private void OnConnectivityChanged(object? sender, ConnectivityEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(AtualizarEstadoConexao);
        }

        private void AtualizarEstadoConexao()
        {
            if (_connectivityService == null) return;

            var isOnline = _connectivityService.IsOnline;
            var tipo = _connectivityService.TipoAtual;

            IndicadorConexao.Color = isOnline
                ? (Color)Application.Current!.Resources["Success"]
                : (Color)Application.Current!.Resources["Danger"];

            LblTipoConexao.Text = tipo switch
            {
                TipoConexao.WiFi => "WiFi",
                TipoConexao.Cellular => "4G",
                TipoConexao.Ethernet => "Ethernet",
                _ => "Offline"
            };

            BtnSync.IsEnabled = isOnline;
            BtnSync.Opacity = isOnline ? 1 : 0.5;
        }

        private void OnSyncProgress(object? sender, SyncProgressEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProgressoSync.IsVisible = true;
                ProgressoSync.Progress = e.Percentual / 100;
            });
        }

        private void OnSyncCompleted(object? sender, SyncCompletedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                ProgressoSync.IsVisible = false;
                await AtualizarContadorPendentesAsync();
            });
        }

        private async Task AtualizarContadorPendentesAsync()
        {
            if (_storageService == null) return;

            try
            {
                var pendentes = await _storageService.ContarLeiturasNaoSincronizadasAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FramePendentes.IsVisible = pendentes > 0;
                    LblPendentes.Text = pendentes.ToString();

                    // Mudar cor baseado na quantidade
                    FramePendentes.BackgroundColor = pendentes switch
                    {
                        < 10 => (Color)Application.Current!.Resources["Warning"],
                        < 50 => (Color)Application.Current!.Resources["Secondary"],
                        _ => (Color)Application.Current!.Resources["Danger"]
                    };
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncBar] Erro: {ex.Message}");
            }
        }

        private async void OnSyncClicked(object? sender, EventArgs e)
        {
            if (_syncService == null || _syncService.IsSyncing)
                return;

            try
            {
                // Animação de rotação
                await BtnSync.RotateTo(360, 500);
                BtnSync.Rotation = 0;

                await _syncService.ForcarSyncAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
        }

        public void Cleanup()
        {
            _updateTimer?.Dispose();

            if (_connectivityService != null)
                _connectivityService.ConnectivityChanged -= OnConnectivityChanged;

            if (_syncService != null)
            {
                _syncService.SyncProgress -= OnSyncProgress;
                _syncService.SyncCompleted -= OnSyncCompleted;
            }
        }
    }
}