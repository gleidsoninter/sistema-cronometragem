using Android.Telephony;
using AppColetor.Models.Entities;
using AppColetor.Services.Implementations;
using AppColetor.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using System.Collections.ObjectModel;
using static Android.Icu.Text.CaseMap;

namespace AppColetor.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly ISerialService _serialService;
        private readonly IApiService _apiService;
        private readonly IStorageService _storageService;
        private readonly IConfigService _configService;
        private readonly IParserService _parserService;
        private readonly SyncBackgroundService _syncService;
        private readonly TimeSyncService _timeSyncService;

        public MainViewModel(
            ISerialService serialService,
            IApiService apiService,
            IStorageService storageService,
            IConfigService configService,
            IParserService parserService,
            SyncBackgroundService syncService,
            TimeSyncService timeSyncService)
        {
            _serialService = serialService;
            _apiService = apiService;
            _storageService = storageService;
            _configService = configService;
            _syncService = syncService;

            Title = "Coletor";

            // Inscrever nos eventos
            _serialService.DataReceived += OnSerialDataReceived;
            _serialService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _serialService.ErrorOccurred += OnSerialError;
            _parserService = parserService;

            // Eventos API
            _apiService.StatusChanged += OnApiStatusChanged;

            // Eventos sync
            _syncService.SyncCompleted += OnSyncCompleted;
            _syncService.SyncFailed += OnSyncFailed;
            _timeSyncService = timeSyncService;
        }

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES OBSERVÁVEIS
        // ═══════════════════════════════════════════════════════════════════

        [ObservableProperty]
        private bool _isUsbConnected;

        [ObservableProperty]
        private bool _isApiConnected;

        [ObservableProperty]
        private string _usbStatus = "Desconectado";

        [ObservableProperty]
        private string _apiStatus = "Offline";

        [ObservableProperty]
        private string _dispositivoConectado = "Nenhum";

        [ObservableProperty]
        private int _idEtapa;

        [ObservableProperty]
        private int _totalLeituras;

        [ObservableProperty]
        private int _leiturasPendentes;

        [ObservableProperty]
        private Leitura? _ultimaLeitura;

        [ObservableProperty]
        private string _ultimaLeituraTexto = "Aguardando...";

        [ObservableProperty]
        private bool _isColetando;

        [ObservableProperty]
        private bool _isSyncing;

        public ObservableCollection<Leitura> LeiturasRecentes { get; } = new();

        public ObservableCollection<SerialDeviceInfo> DispositivosUsb { get; } = new();

        [ObservableProperty]
        private SerialDeviceInfo? _dispositivoSelecionado;

        // ═══════════════════════════════════════════════════════════════════
        // COMANDOS
        // ═══════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task InicializarAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Carregar configurações
                IdEtapa = await _configService.GetIntAsync(Constants.KEY_ID_ETAPA);

                var apiUrl = await _configService.GetStringAsync(Constants.KEY_API_URL);
                var apiToken = await _configService.GetStringAsync(Constants.KEY_API_TOKEN);

                // Configurar API
                _apiService.ConfigurarUrl(apiUrl);
                if (!string.IsNullOrEmpty(apiToken))
                {
                    _apiService.ConfigurarToken(apiToken);
                }

                // Verificar conexão com API
                await VerificarApiAsync();

                // Carregar estatísticas
                await AtualizarEstatisticasAsync();

                // Listar dispositivos USB
                await ListarDispositivosAsync();

                // Iniciar serviço de sync
                if (Preferences.Get("sync_automatico", true))
                {
                    _syncService.Iniciar();
                }

            }, "Inicializando...");
        }

        [RelayCommand]
        private async Task ListarDispositivosAsync()
        {
            var dispositivos = await _serialService.ListarDispositivosAsync();

            DispositivosUsb.Clear();
            foreach (var d in dispositivos)
            {
                DispositivosUsb.Add(d);
            }

            if (DispositivosUsb.Count == 0)
            {
                UsbStatus = "Nenhum dispositivo";
            }
            else
            {
                UsbStatus = $"{DispositivosUsb.Count} dispositivo(s)";
            }
        }

        [RelayCommand]
        private async Task ConectarUsbAsync()
        {
            if (DispositivoSelecionado == null)
            {
                await Shell.Current.DisplayAlert("Aviso", "Selecione um dispositivo USB", "OK");
                return;
            }

            await ExecuteAsync(async () =>
            {
                // Solicitar permissão
                var temPermissao = await _serialService.SolicitarPermissaoAsync(DispositivoSelecionado);
                if (!temPermissao)
                {
                    await Shell.Current.DisplayAlert("Erro", "Permissão USB negada", "OK");
                    return;
                }

                // Conectar
                var baudRate = await _configService.GetIntAsync(
                    Helpers.Constants.KEY_BAUD_RATE,
                    Helpers.Constants.BAUD_RATE_DEFAULT);

                var config = new SerialConfig
                {
                    BaudRate = baudRate
                };

                var conectou = await _serialService.ConectarAsync(DispositivoSelecionado, config);

                if (conectou)
                {
                    IsUsbConnected = true;
                    UsbStatus = "Conectado";
                    DispositivoConectado = DispositivoSelecionado.DisplayName;
                    IsColetando = true;

                    // Vibrar para confirmar
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erro", "Falha ao conectar", "OK");
                }

            }, "Conectando...");
        }

        [RelayCommand]
        private async Task DesconectarUsbAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _serialService.DesconectarAsync();

                IsUsbConnected = false;
                IsColetando = false;
                UsbStatus = "Desconectado";
                DispositivoConectado = "Nenhum";

            }, "Desconectando...");
        }

        [RelayCommand]
        private async Task SincronizarAsync()
        {
            if (IsSyncing)
                return;

            await ExecuteAsync(async () =>
            {
                IsSyncing = true;

                try
                {
                    // Verificar conexão
                    if (!_apiService.IsConnected)
                    {
                        var conectou = await _apiService.VerificarConexaoAsync();

                        if (!conectou)
                        {
                            await Shell.Current.DisplayAlert("Erro", "Sem conexão com a API", "OK");
                            return;
                        }
                    }

                    await _syncService.ForcarSincronizacaoAsync();
                }
                finally
                {
                    IsSyncing = false;
                }

            }, "Sincronizando...");
        }

        [RelayCommand]
        private async Task VerificarApiAsync()
        {
            try
            {
                IsApiConnected = await _apiService.VerificarConexaoAsync();
                ApiStatus = IsApiConnected ? "Online" : "Offline";
            }
            catch
            {
                IsApiConnected = false;
                ApiStatus = "Erro";
            }
        }

        [RelayCommand]
        private async Task AbrirConfiguracoesAsync()
        {
            await NavigateToAsync("//configuracao");
        }

        [RelayCommand]
        private async Task AbrirHistoricoAsync()
        {
            await NavigateToAsync("//historico");
        }

        [RelayCommand]
        private async Task ToggleUsbAsync()
        {
            if (IsUsbConnected)
            {
                await DesconectarUsbCommand.ExecuteAsync(null);
            }
            else
            {
                await MostrarSeletorDispositivoAsync();
            }
        }

        // Handler de status da API
        private void OnApiStatusChanged(object? sender, ApiStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsApiConnected = e.IsConnected && e.IsAuthenticated;
                ApiStatus = e.IsConnected
                    ? (e.IsAuthenticated ? "Online" : "Não autenticado")
                    : "Offline";

                if (!string.IsNullOrEmpty(e.Message))
                {
                    StatusMessage = e.Message;
                }
            });
        }

        // Handler de sync completado
        private void OnSyncCompleted(object? sender, SyncEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await AtualizarEstatisticasAsync();

                if (e.TotalSincronizadas > 0)
                {
                    StatusMessage = $"✅ {e.TotalSincronizadas} leituras sincronizadas";
                }
            });
        }

        // Handler de sync falhou
        private void OnSyncFailed(object? sender, SyncEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"⚠️ Sync falhou: {e.Erro}";
            });
        }

        private async Task MostrarSeletorDispositivoAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Listar dispositivos
                var dispositivos = await _serialService.ListarDispositivosAsync();

                if (dispositivos.Count == 0)
                {
                    await Shell.Current.DisplayAlert(
                        "Nenhum Dispositivo",
                        "Nenhum dispositivo USB Serial encontrado.\n\nVerifique se o coletor está conectado.",
                        "OK");
                    return;
                }

                // Se só tem um, conectar direto
                if (dispositivos.Count == 1)
                {
                    DispositivoSelecionado = dispositivos[0];
                    await ConectarUsbCommand.ExecuteAsync(null);
                    return;
                }

                // Se tem vários, mostrar seletor
                var opcoes = dispositivos.Select(d => d.DisplayName).ToArray();
                var selecionado = await Shell.Current.DisplayActionSheet(
                    "Selecionar Dispositivo",
                    "Cancelar",
                    null,
                    opcoes);

                if (string.IsNullOrEmpty(selecionado) || selecionado == "Cancelar")
                    return;

                DispositivoSelecionado = dispositivos.First(d => d.DisplayName == selecionado);
                await ConectarUsbCommand.ExecuteAsync(null);

            }, "Buscando dispositivos...");
        }

        [RelayCommand]
        private async Task LeituraManualAsync()
        {
            var resultado = await Shell.Current.DisplayPromptAsync(
                "Leitura Manual",
                "Digite o número da moto:",
                "Registrar",
                "Cancelar",
                "Ex: 42",
                maxLength: 4,
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(resultado))
                return;

            if (!int.TryParse(resultado, out int numeroMoto) || numeroMoto <= 0)
            {
                await Shell.Current.DisplayAlert("Erro", "Número inválido", "OK");
                return;
            }

            // Criar leitura manual
            var leitura = new Leitura
            {
                NumeroMoto = numeroMoto,
                Timestamp = DateTime.UtcNow,
                Tipo = "P",
                IdEtapa = IdEtapa,
                DeviceId = await _configService.GetStringAsync(Constants.KEY_DEVICE_ID, "MANUAL"),
                DadosBrutos = $"MANUAL:{numeroMoto}"
            };
            leitura.GerarHash();

            // Salvar
            await _storageService.SalvarLeituraAsync(leitura);

            // Atualizar UI
            UltimaLeitura = leitura;
            UltimaLeituraTexto = $"#{leitura.NumeroMoto} - {leitura.Timestamp:HH:mm:ss.fff}";
            LeiturasRecentes.Insert(0, leitura);
            TotalLeituras++;
            LeiturasPendentes++;

            // Feedback
            try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100)); } catch { }

            StatusMessage = $"✅ Leitura manual #{numeroMoto} registrada";
        }

        [RelayCommand]
        private async Task EditarUltimaLeituraAsync()
        {
            if (UltimaLeitura == null) return;
            await EditarLeituraAsync(UltimaLeitura);
        }

        [RelayCommand]
        private async Task EditarLeituraAsync(Leitura leitura)
        {
            if (leitura == null) return;

            // Não permitir editar se já sincronizado
            if (leitura.Sincronizado)
            {
                await Shell.Current.DisplayAlert(
                    "Não permitido",
                    "Esta leitura já foi sincronizada e não pode ser editada.",
                    "OK");
                return;
            }

            var novoNumero = await Shell.Current.DisplayPromptAsync(
                "Corrigir Número",
                $"Número atual: {leitura.NumeroMoto}\nDigite o número correto:",
                "Salvar",
                "Cancelar",
                leitura.NumeroMoto.ToString(),
                maxLength: 4,
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(novoNumero))
                return;

            if (!int.TryParse(novoNumero, out int numero) || numero <= 0)
            {
                await Shell.Current.DisplayAlert("Erro", "Número inválido", "OK");
                return;
            }

            // Atualizar leitura
            var numeroAntigo = leitura.NumeroMoto;
            leitura.NumeroMoto = numero;
            leitura.DadosBrutos = $"{leitura.DadosBrutos} [CORRIGIDO:{numeroAntigo}->{numero}]";
            leitura.GerarHash(); // Regenerar hash

            // Salvar
            await _storageService.SalvarLeituraAsync(leitura);

            // Atualizar UI se for a última
            if (UltimaLeitura?.Id == leitura.Id)
            {
                UltimaLeituraTexto = $"#{leitura.NumeroMoto} - {leitura.Timestamp:HH:mm:ss.fff}";
            }

            StatusMessage = $"✅ Corrigido: #{numeroAntigo} → #{numero}";
        }

        // ═══════════════════════════════════════════════════════════════════
        // MÉTODOS PRIVADOS
        // ═══════════════════════════════════════════════════════════════════

        private async Task AtualizarEstatisticasAsync()
        {
            TotalLeituras = await _storageService.ContarLeiturasAsync();
            LeiturasPendentes = await _storageService.ContarLeiturasNaoSincronizadasAsync();
        }

        private async void OnSerialDataReceived(object? sender, SerialDataEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Main] Dados recebidos: {e.Data}");

                // Obter protocolo configurado
                var protocolo = await _configService.GetStringAsync(
                    Constants.KEY_PROTOCOLO,
                    Constants.PROTOCOLO_GENERICO);

                // Parsear dados
                var parseResult = _parserService.Parsear(e.Data, protocolo);

                if (!parseResult.Sucesso)
                {
                    System.Diagnostics.Debug.WriteLine($"[Main] Falha no parsing: {parseResult.Erro}");

                    // Registrar dado não parseado (para debug)
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = $"⚠️ Dado não reconhecido: {e.Data}";
                    });

                    return;
                }

                var leitura = parseResult.Leitura!;

                // Preencher dados adicionais
                leitura.IdEtapa = IdEtapa;
                leitura.DeviceId = await _configService.GetStringAsync(Constants.KEY_DEVICE_ID, "COLETOR-001");
                leitura.DadosBrutos = e.Data;
                leitura.GerarHash();

                // Validar
                var validationResult = _parserService.Validar(leitura);

                if (!validationResult.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Main] Validação falhou: {string.Join(", ", validationResult.Erros)}");

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = $"⚠️ Leitura inválida: {validationResult.Erros.First()}";
                    });

                    return;
                }

                // Verificar duplicata
                if (await _storageService.ExisteLeituraAsync(leitura.Hash))
                {
                    System.Diagnostics.Debug.WriteLine($"[Main] Leitura duplicada ignorada: {leitura.Hash}");
                    return;
                }

                // Salvar localmente
                var savedId = await _storageService.SalvarLeituraAsync(leitura);
                leitura.Id = savedId;

                System.Diagnostics.Debug.WriteLine(
                    $"[Main] Leitura salva: #{leitura.NumeroMoto} @ {leitura.Timestamp:HH:mm:ss.fff}");

                // Atualizar UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UltimaLeitura = leitura;
                    UltimaLeituraTexto = $"#{leitura.NumeroMoto} - {leitura.Timestamp:HH:mm:ss.fff}";

                    LeiturasRecentes.Insert(0, leitura);
                    if (LeiturasRecentes.Count > 20)
                        LeiturasRecentes.RemoveAt(LeiturasRecentes.Count - 1);

                    TotalLeituras++;
                    LeiturasPendentes++;

                    StatusMessage = $"✅ #{leitura.NumeroMoto}";
                });

                // Feedback haptico
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50));
                }
                catch { }

                // Feedback sonoro (se implementado)
                // await _audioService.PlayBeepAsync();

                // Tentar enviar para API (em background)
                if (IsApiConnected)
                {
                    _ = EnviarParaApiAsync(leitura);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Main] Erro ao processar leitura: {ex}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = $"❌ Erro: {ex.Message}";
                });
            }
        }

        private async Task EnviarParaApiAsync(Leitura leitura)
        {
            if (!_apiService.IsConnected)
                return;

            try
            {
                var response = await _apiService.EnviarLeituraAsync(leitura);

                if (response != null)
                {
                    if (response.Status == "OK" || response.Status == "DUPLICADA")
                    {
                        await _storageService.MarcarComoSincronizadaAsync(leitura.Id);

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            LeiturasPendentes = Math.Max(0, LeiturasPendentes - 1);

                            // Atualizar leitura na lista
                            leitura.Sincronizado = true;
                            leitura.RespostaApi = response.TempoFormatado;
                            leitura.DataSincronizacao = DateTime.UtcNow;
                        });

                        System.Diagnostics.Debug.WriteLine(
                            $"[Main] Leitura #{leitura.NumeroMoto} sincronizada em tempo real");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Main] Leitura #{leitura.NumeroMoto} não sincronizada: {response.Mensagem}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Main] Erro ao sincronizar em tempo real: {ex.Message}");
                // Será sincronizado pelo serviço de background
            }
        }

        private Leitura? ParsearLeitura(string dados)
        {
            if (string.IsNullOrWhiteSpace(dados))
                return null;

            var protocolo = _configService.GetStringAsync(
                Helpers.Constants.KEY_PROTOCOLO,
                Helpers.Constants.PROTOCOLO_GENERICO).Result;

            return protocolo switch
            {
                Helpers.Constants.PROTOCOLO_GENERICO => ParsearGenerico(dados),
                Helpers.Constants.PROTOCOLO_RF_TIMING => ParsearRfTiming(dados),
                Helpers.Constants.PROTOCOLO_AMB => ParsearAmb(dados),
                _ => ParsearGenerico(dados)
            };
        }


        // No MainViewModel, atualizar OnSerialDataReceived:


        private async Task TentarEnviarParaApiAsync(Leitura leitura)
        {
            try
            {
                var response = await _apiService.EnviarLeituraAsync(leitura);

                if (response != null)
                {
                    // Resolver possíveis conflitos
                    var conflito = new LeituraConflito
                    {
                        Status = response.Status,
                        Mensagem = response.Mensagem
                    };

                    var resolucao = _conflictResolver.ResolverConflito(leitura, conflito);

                    if (resolucao.Estrategia == EstrategiaResolucao.MarcarSincronizada)
                    {
                        await _storageService.MarcarComoSincronizadaAsync(leitura.Id);

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            LeiturasPendentes = Math.Max(0, LeiturasPendentes - 1);
                            leitura.Sincronizado = true;
                            leitura.RespostaApi = response.TempoFormatado;

                            StatusMessage = $"✅ #{leitura.NumeroMoto} sincronizado";
                        });

                        await _feedbackService.LeituraSincronizadaAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Main] Erro ao enviar para API: {ex.Message}");
                // Será sincronizado pelo serviço de background
            }
        }

        /// <summary>
        /// Formato: NUMERO,TIMESTAMP ou NUMERO
        /// </summary>
        private Leitura? ParsearGenerico(string dados)
        {
            dados = dados.Trim();
            var partes = dados.Split(',');

            if (partes.Length < 1) return null;

            if (!int.TryParse(partes[0].Trim(), out int numeroMoto))
                return null;

            var timestamp = DateTime.UtcNow;
            if (partes.Length > 1 && DateTime.TryParse(partes[1].Trim(), out DateTime parsed))
            {
                timestamp = parsed.ToUniversalTime();
            }

            return new Leitura
            {
                NumeroMoto = numeroMoto,
                Timestamp = timestamp,
                Tipo = "P"
            };
        }

        /// <summary>
        /// Formato: #MOTO:TEMPO:VOLTA#
        /// </summary>
        private Leitura? ParsearRfTiming(string dados)
        {
            dados = dados.Trim().Trim('#');
            var partes = dados.Split(':');

            if (partes.Length < 2) return null;

            if (!int.TryParse(partes[0], out int numeroMoto))
                return null;

            int? volta = null;
            if (partes.Length > 2 && int.TryParse(partes[2], out int v))
                volta = v;

            return new Leitura
            {
                NumeroMoto = numeroMoto,
                Timestamp = DateTime.UtcNow,
                Tipo = "P",
                Volta = volta
            };
        }

        /// <summary>
        /// Formato: @T:TRANS_ID:LOOP:TIME:HITS
        /// </summary>
        private Leitura? ParsearAmb(string dados)
        {
            dados = dados.Trim();
            if (!dados.StartsWith("@T:")) return null;

            var partes = dados[3..].Split(':');
            if (partes.Length < 3) return null;

            if (!int.TryParse(partes[0], out int transponderId))
                return null;

            // O transponder ID precisaria ser mapeado para número da moto
            // Por simplicidade, usamos o ID diretamente

            return new Leitura
            {
                NumeroMoto = transponderId,
                Timestamp = DateTime.UtcNow,
                Tipo = "P"
            };
        }

        private void OnConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsUsbConnected = e.Status == ConnectionStatus.Connected;
                UsbStatus = e.Status switch
                {
                    ConnectionStatus.Connected => "Conectado",
                    ConnectionStatus.Connecting => "Conectando...",
                    ConnectionStatus.Disconnected => "Desconectado",
                    ConnectionStatus.Reconnecting => "Reconectando...",
                    ConnectionStatus.Error => "Erro",
                    _ => "Desconhecido"
                };
            });
        }

        private void OnSerialError(object? sender, SerialErrorEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                HasError = true;
                ErrorMessage = e.Message;

                if (e.IsFatal)
                {
                    IsUsbConnected = false;
                    IsColetando = false;
                    UsbStatus = "Erro Fatal";

                    await Shell.Current.DisplayAlert("Erro USB", e.Message, "OK");
                }
            });
        }
    }
}