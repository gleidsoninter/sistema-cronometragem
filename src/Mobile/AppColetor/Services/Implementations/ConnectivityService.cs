using AppColetor.Helpers;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class ConnectivityService : IConnectivityService, IDisposable
    {
        private readonly IConfigService _configService;
        private Timer? _pingTimer;
        private bool _isOnline;
        private bool _apiReachable;
        private TipoConexao _tipoAtual = TipoConexao.Nenhuma;
        private bool _disposed;

        private const int PING_INTERVAL_MS = 30000; // 30 segundos

        public event EventHandler<ConnectivityEventArgs>? ConnectivityChanged;

        public bool IsOnline => _isOnline && _apiReachable;
        public bool IsWiFi => _tipoAtual == TipoConexao.WiFi;
        public bool IsCellular => _tipoAtual == TipoConexao.Cellular;
        public TipoConexao TipoAtual => _tipoAtual;

        public int QualidadeConexao
        {
            get
            {
                if (!_isOnline) return 0;
                if (!_apiReachable) return 25;

                return _tipoAtual switch
                {
                    TipoConexao.WiFi => 100,
                    TipoConexao.Ethernet => 100,
                    TipoConexao.Cellular => 75,
                    _ => 50
                };
            }
        }

        public ConnectivityService(IConfigService configService)
        {
            _configService = configService;

            // Registrar evento de mudança de conectividade
            Connectivity.ConnectivityChanged += OnConnectivityChanged;

            // Verificar estado inicial
            AtualizarEstadoConectividade();
        }

        public void IniciarMonitoramento()
        {
            PararMonitoramento();

            // Timer para verificar alcance da API periodicamente
            _pingTimer = new Timer(
                async _ => await VerificarAlcanceApiAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(PING_INTERVAL_MS));

            System.Diagnostics.Debug.WriteLine("[Connectivity] Monitoramento iniciado");
        }

        public void PararMonitoramento()
        {
            _pingTimer?.Dispose();
            _pingTimer = null;

            System.Diagnostics.Debug.WriteLine("[Connectivity] Monitoramento parado");
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            var tipoAnterior = _tipoAtual;
            AtualizarEstadoConectividade();

            System.Diagnostics.Debug.WriteLine(
                $"[Connectivity] Mudança detectada: {tipoAnterior} → {_tipoAtual}, " +
                $"Online: {_isOnline}");

            // Disparar evento
            ConnectivityChanged?.Invoke(this, new ConnectivityEventArgs
            {
                IsOnline = _isOnline,
                TipoAnterior = tipoAnterior,
                TipoAtual = _tipoAtual,
                Mensagem = _isOnline ? "Conectado" : "Sem conexão"
            });

            // Se reconectou, verificar alcance da API
            if (_isOnline && !_apiReachable)
            {
                _ = VerificarAlcanceApiAsync();
            }
        }

        private void AtualizarEstadoConectividade()
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            var profiles = Connectivity.Current.ConnectionProfiles;

            _isOnline = networkAccess == NetworkAccess.Internet;

            // Determinar tipo de conexão
            if (!_isOnline)
            {
                _tipoAtual = TipoConexao.Nenhuma;
            }
            else if (profiles.Contains(ConnectionProfile.WiFi))
            {
                _tipoAtual = TipoConexao.WiFi;
            }
            else if (profiles.Contains(ConnectionProfile.Cellular))
            {
                _tipoAtual = TipoConexao.Cellular;
            }
            else if (profiles.Contains(ConnectionProfile.Ethernet))
            {
                _tipoAtual = TipoConexao.Ethernet;
            }
            else
            {
                _tipoAtual = TipoConexao.Desconhecido;
            }
        }

        public async Task<bool> VerificarAlcanceApiAsync(CancellationToken cancellationToken = default)
        {
            if (!_isOnline)
            {
                _apiReachable = false;
                return false;
            }

            try
            {
                var apiUrl = await _configService.GetStringAsync(Constants.KEY_API_URL);

                if (string.IsNullOrEmpty(apiUrl))
                {
                    _apiReachable = false;
                    return false;
                }

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

                var response = await httpClient.GetAsync(
                    $"{apiUrl}/api/v1/health",
                    cancellationToken);

                var wasReachable = _apiReachable;
                _apiReachable = response.IsSuccessStatusCode;

                // Se mudou de não alcançável para alcançável, notificar
                if (!wasReachable && _apiReachable)
                {
                    System.Diagnostics.Debug.WriteLine("[Connectivity] API agora está acessível!");

                    ConnectivityChanged?.Invoke(this, new ConnectivityEventArgs
                    {
                        IsOnline = true,
                        TipoAnterior = _tipoAtual,
                        TipoAtual = _tipoAtual,
                        Mensagem = "API acessível"
                    });
                }

                return _apiReachable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Connectivity] Erro ao verificar API: {ex.Message}");
                _apiReachable = false;
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Connectivity.ConnectivityChanged -= OnConnectivityChanged;
            PararMonitoramento();
        }
    }
}