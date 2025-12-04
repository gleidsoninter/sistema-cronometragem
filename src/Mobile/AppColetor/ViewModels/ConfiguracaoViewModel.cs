using AppColetor.Helpers;
using AppColetor.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using System.Collections.ObjectModel;

namespace AppColetor.ViewModels
{
    public partial class ConfiguracaoViewModel : BaseViewModel
    {
        private readonly IConfigService _configService;
        private readonly ISerialService _serialService;
        private readonly IApiService _apiService;

        public ConfiguracaoViewModel(
            IConfigService configService,
            ISerialService serialService,
            IApiService apiService)
        {
            _configService = configService;
            _serialService = serialService;
            _apiService = apiService;

            Title = "Configurações";

            // Opções de baud rate
            BaudRates = new ObservableCollection<int>(Constants.BAUD_RATES);

            // Opções de protocolo
            Protocolos = new ObservableCollection<string>(Constants.PROTOCOLOS);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES - API
        // ═══════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private string _apiUrl = "";

        [ObservableProperty]
        private string _apiToken = "";

        [ObservableProperty]
        private bool _apiConectada;

        [ObservableProperty]
        private string _apiStatus = "Não testada";

        [ObservableProperty]
        private string _authStatus = "";

        [ObservableProperty]
        private string _senhaDispositivo = "";


        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES - SERIAL
        // ═══════════════════════════════════════════════════════════════════

        public ObservableCollection<int> BaudRates { get; }

        [ObservableProperty]
        private int _baudRateSelecionado = 115200;

        public ObservableCollection<string> Protocolos { get; }

        [ObservableProperty]
        private string _protocoloSelecionado = Constants.PROTOCOLO_GENERICO;

        [ObservableProperty]
        private string _terminadorLinha = "\\r\\n";

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES - ETAPA
        // ═══════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private int _idEtapa;

        [ObservableProperty]
        private string _deviceId = "";

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES - FEEDBACK
        // ═══════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _vibrarAoLer = true;

        [ObservableProperty]
        private bool _somAoLer = true;

        [ObservableProperty]
        private bool _manterTelaLigada = true;

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES - SYNC
        // ═══════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _syncAutomatico = true;

        [ObservableProperty]
        private int _intervaloSyncSegundos = 5;

        // ═══════════════════════════════════════════════════════════════════
        // COMANDOS
        // ═══════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task AutenticarAsync()
        {
            await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(DeviceId))
                {
                    await Shell.Current.DisplayAlert("Erro", "Device ID é obrigatório", "OK");
                    return;
                }

                // Solicitar senha
                var senha = await Shell.Current.DisplayPromptAsync(
                    "Autenticação",
                    $"Digite a senha do dispositivo {DeviceId}:",
                    "Autenticar",
                    "Cancelar",
                    placeholder: "Senha",
                    maxLength: 50,
                    keyboard: Keyboard.Default);

                if (string.IsNullOrEmpty(senha))
                    return;

                AuthStatus = "Autenticando...";

                // Configurar URL
                _apiService.ConfigurarUrl(ApiUrl);

                // Autenticar
                var result = await _apiService.AutenticarDispositivoAsync(DeviceId, senha);

                if (result?.Sucesso == true)
                {
                    AuthStatus = "✅ Autenticado com sucesso!";
                    ApiToken = result.Token ?? "";

                    // Salvar token de forma segura
                    await SecureStorage.SetAsync(Constants.KEY_API_TOKEN, ApiToken);
                    await SecureStorage.SetAsync("device_senha", senha);

                    // Configurar token
                    _apiService.ConfigurarToken(ApiToken);

                    ApiConectada = true;
                    ApiStatus = "✅ Autenticado";
                }
                else
                {
                    AuthStatus = $"❌ {result?.Mensagem ?? "Falha na autenticação"}";
                    ApiConectada = false;
                    ApiStatus = "❌ Não autenticado";
                }

            }, "Autenticando...");
        }

        [RelayCommand]
        private async Task CarregarAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Carregar configurações salvas
                ApiUrl = await _configService.GetStringAsync(Constants.KEY_API_URL, Constants.API_BASE_URL_DEFAULT);
                ApiToken = await _configService.GetStringAsync(Constants.KEY_API_TOKEN, "");
                BaudRateSelecionado = await _configService.GetIntAsync(Constants.KEY_BAUD_RATE, Constants.BAUD_RATE_DEFAULT);
                ProtocoloSelecionado = await _configService.GetStringAsync(Constants.KEY_PROTOCOLO, Constants.PROTOCOLO_GENERICO);
                IdEtapa = await _configService.GetIntAsync(Constants.KEY_ID_ETAPA, 0);
                DeviceId = await _configService.GetStringAsync(Constants.KEY_DEVICE_ID, GerarDeviceId());

                // Carregar preferências
                VibrarAoLer = Preferences.Get("vibrar_ao_ler", true);
                SomAoLer = Preferences.Get("som_ao_ler", true);
                ManterTelaLigada = Preferences.Get("manter_tela_ligada", true);
                SyncAutomatico = Preferences.Get("sync_automatico", true);
                IntervaloSyncSegundos = Preferences.Get("intervalo_sync", 5);

            }, "Carregando configurações...");
        }

        [RelayCommand]
        private async Task SalvarAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Validar
                if (IdEtapa <= 0)
                {
                    await Shell.Current.DisplayAlert("Erro", "ID da Etapa é obrigatório", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ApiUrl))
                {
                    await Shell.Current.DisplayAlert("Erro", "URL da API é obrigatória", "OK");
                    return;
                }

                // Salvar configurações
                await _configService.SetStringAsync(Constants.KEY_API_URL, ApiUrl.Trim());
                await _configService.SetStringAsync(Constants.KEY_API_TOKEN, ApiToken?.Trim() ?? "");
                await _configService.SetIntAsync(Constants.KEY_BAUD_RATE, BaudRateSelecionado);
                await _configService.SetStringAsync(Constants.KEY_PROTOCOLO, ProtocoloSelecionado);
                await _configService.SetIntAsync(Constants.KEY_ID_ETAPA, IdEtapa);
                await _configService.SetStringAsync(Constants.KEY_DEVICE_ID, DeviceId);

                // Salvar preferências
                Preferences.Set("vibrar_ao_ler", VibrarAoLer);
                Preferences.Set("som_ao_ler", SomAoLer);
                Preferences.Set("manter_tela_ligada", ManterTelaLigada);
                Preferences.Set("sync_automatico", SyncAutomatico);
                Preferences.Set("intervalo_sync", IntervaloSyncSegundos);

                StatusMessage = "✅ Configurações salvas!";

                await Task.Delay(1500);
                await GoBackAsync();

            }, "Salvando...");
        }

        [RelayCommand]
        private async Task TestarApiAsync()
        {
            await ExecuteAsync(async () =>
            {
                ApiStatus = "Testando...";

                // Configurar URL temporariamente
                _apiService.ConfigurarUrl(ApiUrl);

                var conectou = await _apiService.VerificarConexaoAsync();

                if (conectou)
                {
                    ApiConectada = true;
                    ApiStatus = "✅ Conectada";
                }
                else
                {
                    ApiConectada = false;
                    ApiStatus = "❌ Falha na conexão";
                }

            }, "Testando conexão...");
        }

        [RelayCommand]
        private async Task ScanQrCodeAsync()
        {
            // TODO: Implementar leitura de QR Code para configuração rápida
            await Shell.Current.DisplayAlert("Em breve",
                "Funcionalidade de QR Code será implementada em breve", "OK");
        }

        [RelayCommand]
        private void GerarNovoDeviceId()
        {
            DeviceId = GerarDeviceId();
        }

        private string GerarDeviceId()
        {
            return $"COLETOR-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }

        [RelayCommand]
        private async Task GerarDiagnosticoAsync()
        {
            await DiagnosticoHelper.CompartilharRelatorioAsync();
        }
    }
}