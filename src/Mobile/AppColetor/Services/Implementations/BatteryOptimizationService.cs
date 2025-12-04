using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class BatteryOptimizationService : IDisposable
    {
        private readonly IConfigService _configService;
        private readonly IConnectivityService _connectivityService;

        private BatteryMode _currentMode = BatteryMode.Normal;
        private bool _isMonitoring;

        public event EventHandler<BatteryModeChangedEventArgs>? ModeChanged;

        public BatteryMode CurrentMode => _currentMode;

        public BatteryOptimizationService(
            IConfigService configService,
            IConnectivityService connectivityService)
        {
            _configService = configService;
            _connectivityService = connectivityService;
        }

        /// <summary>
        /// Inicia monitoramento de bateria
        /// </summary>
        public void IniciarMonitoramento()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            Battery.Default.BatteryInfoChanged += OnBatteryInfoChanged;

            // Verificar nível atual
            AtualizarModo();

            System.Diagnostics.Debug.WriteLine("[Battery] Monitoramento iniciado");
        }

        /// <summary>
        /// Para monitoramento
        /// </summary>
        public void PararMonitoramento()
        {
            if (!_isMonitoring) return;

            _isMonitoring = false;
            Battery.Default.BatteryInfoChanged -= OnBatteryInfoChanged;

            System.Diagnostics.Debug.WriteLine("[Battery] Monitoramento parado");
        }

        private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
        {
            AtualizarModo();
        }

        private void AtualizarModo()
        {
            var nivel = Battery.Default.ChargeLevel;
            var estado = Battery.Default.State;

            var novoModo = DeterminarModo(nivel, estado);

            if (novoModo != _currentMode)
            {
                var modoAnterior = _currentMode;
                _currentMode = novoModo;

                System.Diagnostics.Debug.WriteLine(
                    $"[Battery] Modo alterado: {modoAnterior} → {novoModo} (Nível: {nivel * 100:F0}%)");

                ModeChanged?.Invoke(this, new BatteryModeChangedEventArgs
                {
                    ModoAnterior = modoAnterior,
                    ModoAtual = novoModo,
                    NivelBateria = nivel
                });
            }
        }

        private BatteryMode DeterminarModo(double nivel, BatteryState estado)
        {
            // Se está carregando, modo normal
            if (estado == BatteryState.Charging || estado == BatteryState.Full)
            {
                return BatteryMode.Normal;
            }

            // Determinar modo baseado no nível
            return nivel switch
            {
                >= 0.50 => BatteryMode.Normal,
                >= 0.20 => BatteryMode.Economy,
                _ => BatteryMode.Critical
            };
        }

        /// <summary>
        /// Obtém configurações otimizadas para o modo atual
        /// </summary>
        public BatteryOptimizedSettings ObterConfiguracoes()
        {
            return _currentMode switch
            {
                BatteryMode.Normal => new BatteryOptimizedSettings
                {
                    IntervaloSyncMs = 5000,
                    IntervaloHeartbeatMs = 30000,
                    TamanhoLoteSync = 50,
                    HabilitarLogs = true,
                    BrilhoTela = 1.0,
                    ManterTelaLigada = true,
                    VibrarLeitura = true
                },
                BatteryMode.Economy => new BatteryOptimizedSettings
                {
                    IntervaloSyncMs = 15000,
                    IntervaloHeartbeatMs = 60000,
                    TamanhoLoteSync = 100,
                    HabilitarLogs = false,
                    BrilhoTela = 0.5,
                    ManterTelaLigada = true,
                    VibrarLeitura = true
                },
                BatteryMode.Critical => new BatteryOptimizedSettings
                {
                    IntervaloSyncMs = 30000,
                    IntervaloHeartbeatMs = 120000,
                    TamanhoLoteSync = 200,
                    HabilitarLogs = false,
                    BrilhoTela = 0.2,
                    ManterTelaLigada = false,
                    VibrarLeitura = false
                },
                _ => new BatteryOptimizedSettings()
            };
        }

        /// <summary>
        /// Estima tempo restante de bateria
        /// </summary>
        public BatteryEstimate EstimarTempoRestante(double consumoPorHora = 0.1)
        {
            var nivel = Battery.Default.ChargeLevel;
            var estado = Battery.Default.State;

            if (estado == BatteryState.Charging || estado == BatteryState.Full)
            {
                return new BatteryEstimate
                {
                    NivelAtual = nivel,
                    Carregando = true,
                    TempoEstimadoHoras = double.MaxValue
                };
            }

            // Ajustar consumo baseado no modo
            var consumoAjustado = _currentMode switch
            {
                BatteryMode.Normal => consumoPorHora,
                BatteryMode.Economy => consumoPorHora * 0.7,
                BatteryMode.Critical => consumoPorHora * 0.5,
                _ => consumoPorHora
            };

            var horasRestantes = nivel / consumoAjustado;

            return new BatteryEstimate
            {
                NivelAtual = nivel,
                Carregando = false,
                TempoEstimadoHoras = horasRestantes,
                ModoAtual = _currentMode
            };
        }

        /// <summary>
        /// Obtém dicas de economia de bateria
        /// </summary>
        public List<string> ObterDicasEconomia()
        {
            var dicas = new List<string>();
            var nivel = Battery.Default.ChargeLevel;

            if (nivel < 0.3)
            {
                dicas.Add("⚠️ Bateria baixa! Conecte o carregador se possível.");
            }

            if (_connectivityService.IsCellular)
            {
                dicas.Add("📶 Conexão 4G consome mais bateria. Use WiFi se disponível.");
            }

            if (Preferences.Get("manter_tela_ligada", true))
            {
                dicas.Add("💡 Desligar 'Manter Tela Ligada' economiza bateria.");
            }

            if (Preferences.Get("vibrar_ao_ler", true))
            {
                dicas.Add("📳 Desativar vibração economiza bateria.");
            }

            if (nivel < 0.5 && _currentMode == BatteryMode.Normal)
            {
                dicas.Add("🔋 Considere ativar modo economia manualmente.");
            }

            return dicas;
        }

        public void Dispose()
        {
            PararMonitoramento();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLASSES AUXILIARES
    // ═══════════════════════════════════════════════════════════════════════

    public enum BatteryMode
    {
        Normal,     // > 50%
        Economy,    // 20-50%
        Critical    // < 20%
    }

    public class BatteryOptimizedSettings
    {
        public int IntervaloSyncMs { get; set; } = 5000;
        public int IntervaloHeartbeatMs { get; set; } = 30000;
        public int TamanhoLoteSync { get; set; } = 50;
        public bool HabilitarLogs { get; set; } = true;
        public double BrilhoTela { get; set; } = 1.0;
        public bool ManterTelaLigada { get; set; } = true;
        public bool VibrarLeitura { get; set; } = true;
    }

    public class BatteryEstimate
    {
        public double NivelAtual { get; set; }
        public bool Carregando { get; set; }
        public double TempoEstimadoHoras { get; set; }
        public BatteryMode ModoAtual { get; set; }

        public string TempoFormatado
        {
            get
            {
                if (Carregando) return "Carregando";
                if (TempoEstimadoHoras >= 24) return ">24h";
                if (TempoEstimadoHoras >= 1) return $"{TempoEstimadoHoras:F1}h";
                return $"{TempoEstimadoHoras * 60:F0}min";
            }
        }
    }

    public class BatteryModeChangedEventArgs : EventArgs
    {
        public BatteryMode ModoAnterior { get; set; }
        public BatteryMode ModoAtual { get; set; }
        public double NivelBateria { get; set; }
    }
}