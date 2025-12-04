using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Hoho.Android.UsbSerial.Driver;
using System.Text;
using AppColetor.Services.Interfaces;

namespace AppColetor.Platforms.Android.Services
{
    /// <summary>
    /// Implementação Android do serviço de comunicação serial USB
    /// </summary>
    public class AndroidSerialService : ISerialService, IDisposable
    {

        private Timer? _watchdogTimer;
        private DateTime _ultimaLeitura = DateTime.MinValue;
        private const int WATCHDOG_INTERVAL_MS = 5000;
        private const int WATCHDOG_TIMEOUT_MS = 30000;

        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════════════════════════════

        private const string ACTION_USB_PERMISSION = "com.cronometragem.coletor.USB_PERMISSION";
        private const int READ_BUFFER_SIZE = 4096;
        private const int READ_TIMEOUT_MS = 100;
        private const int RECONNECT_DELAY_MS = 2000;
        private const int MAX_RECONNECT_ATTEMPTS = 5;

        // ═══════════════════════════════════════════════════════════════════
        // CAMPOS PRIVADOS
        // ═══════════════════════════════════════════════════════════════════

        private readonly Context _context;
        private readonly UsbManager _usbManager;
        private UsbSerialPort? _serialPort;
        private UsbDeviceConnection? _connection;
        private UsbDevice? _currentDevice;

        private CancellationTokenSource? _readCancellationToken;
        private Task? _readTask;

        private readonly StringBuilder _lineBuffer = new();
        private string _lineEnding = "\r\n";

        private bool _isConnected;
        private bool _isReconnecting;
        private int _reconnectAttempts;
        private SerialConfig? _currentConfig;
        private SerialDeviceInfo? _currentDeviceInfo;

        private readonly object _lockObject = new();
        private bool _disposed;

        // Receiver para permissão USB
        private UsbPermissionReceiver? _permissionReceiver;
        private TaskCompletionSource<bool>? _permissionTcs;

        // ═══════════════════════════════════════════════════════════════════
        // EVENTOS
        // ═══════════════════════════════════════════════════════════════════

        public event EventHandler<SerialDataEventArgs>? DataReceived;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<SerialErrorEventArgs>? ErrorOccurred;

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES
        // ═══════════════════════════════════════════════════════════════════

        public bool IsConnected => _isConnected;
        public string? DeviceName => _currentDeviceInfo?.DisplayName;

        // ═══════════════════════════════════════════════════════════════════
        // CONSTRUTOR
        // ═══════════════════════════════════════════════════════════════════

        public AndroidSerialService()
        {
            _context = Platform.AppContext;
            _usbManager = (UsbManager)_context.GetSystemService(Context.UsbService)!;

            // Registrar para eventos de USB da MainActivity
            MainActivity.UsbDeviceAttached += OnUsbDeviceAttached;
            MainActivity.UsbDeviceDetached += OnUsbDeviceDetached;
        }

        // ═══════════════════════════════════════════════════════════════════
        // LISTAR DISPOSITIVOS
        // ═══════════════════════════════════════════════════════════════════

        public Task<List<SerialDeviceInfo>> ListarDispositivosAsync()
        {
            var resultado = new List<SerialDeviceInfo>();

            try
            {
                var deviceList = _usbManager.DeviceList;

                if (deviceList == null || deviceList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[Serial] Nenhum dispositivo USB encontrado");
                    return Task.FromResult(resultado);
                }

                var prober = UsbSerialProber.DefaultProber;

                foreach (var device in deviceList.Values)
                {
                    try
                    {
                        // Tentar obter driver para o dispositivo
                        var driver = prober.ProbeDevice(device);

                        if (driver != null)
                        {
                            var info = new SerialDeviceInfo
                            {
                                DeviceId = device.DeviceId.ToString(),
                                Name = device.ProductName ?? device.DeviceName ?? "Dispositivo USB",
                                VendorId = device.VendorId,
                                ProductId = device.ProductId,
                                ChipType = GetChipType(driver),
                                HasPermission = _usbManager.HasPermission(device)
                            };

                            resultado.Add(info);

                            System.Diagnostics.Debug.WriteLine(
                                $"[Serial] Dispositivo encontrado: {info.DisplayName} " +
                                $"(VID:{info.VendorId:X4} PID:{info.ProductId:X4})");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Serial] Erro ao verificar dispositivo {device.DeviceName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Serial] Erro ao listar dispositivos: {ex.Message}");
                RaiseError("Erro ao listar dispositivos USB", ex, false);
            }

            return Task.FromResult(resultado);
        }

        private string GetChipType(IUsbSerialDriver driver)
        {
            return driver switch
            {
                Ch34xSerialDriver => "CH340",
                FtdiSerialDriver => "FTDI",
                Cp21xxSerialDriver => "CP210x",
                ProlificSerialDriver => "PL2303",
                CdcAcmSerialDriver => "CDC-ACM",
                _ => "Desconhecido"
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // SOLICITAR PERMISSÃO
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> SolicitarPermissaoAsync(SerialDeviceInfo deviceInfo)
        {
            try
            {
                // Encontrar o UsbDevice
                var device = FindUsbDevice(deviceInfo);
                if (device == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Serial] Dispositivo não encontrado");
                    return false;
                }

                // Verificar se já tem permissão
                if (_usbManager.HasPermission(device))
                {
                    System.Diagnostics.Debug.WriteLine("[Serial] Já tem permissão");
                    return true;
                }

                // Criar TaskCompletionSource para aguardar resultado
                _permissionTcs = new TaskCompletionSource<bool>();

                // Registrar receiver
                _permissionReceiver = new UsbPermissionReceiver(OnPermissionResult);
                var filter = new IntentFilter(ACTION_USB_PERMISSION);

                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    _context.RegisterReceiver(_permissionReceiver, filter, ReceiverFlags.Exported);
                }
                else
                {
                    _context.RegisterReceiver(_permissionReceiver, filter);
                }

                // Criar PendingIntent
                var intent = new Intent(ACTION_USB_PERMISSION);
                var flags = PendingIntentFlags.Mutable;
                if (OperatingSystem.IsAndroidVersionAtLeast(31))
                {
                    flags |= PendingIntentFlags.Mutable;
                }
                var pendingIntent = PendingIntent.GetBroadcast(_context, 0, intent, flags);

                // Solicitar permissão
                System.Diagnostics.Debug.WriteLine("[Serial] Solicitando permissão USB...");
                _usbManager.RequestPermission(device, pendingIntent);

                // Aguardar resultado (timeout de 60 segundos)
                var timeoutTask = Task.Delay(60000);
                var completedTask = await Task.WhenAny(_permissionTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine("[Serial] Timeout ao aguardar permissão");
                    return false;
                }

                return await _permissionTcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Serial] Erro ao solicitar permissão: {ex.Message}");
                return false;
            }
            finally
            {
                // Desregistrar receiver
                if (_permissionReceiver != null)
                {
                    try
                    {
                        _context.UnregisterReceiver(_permissionReceiver);
                    }
                    catch { }
                    _permissionReceiver = null;
                }
            }
        }

        private void OnPermissionResult(bool granted)
        {
            System.Diagnostics.Debug.WriteLine($"[Serial] Permissão: {(granted ? "CONCEDIDA" : "NEGADA")}");
            _permissionTcs?.TrySetResult(granted);
        }

        private UsbDevice? FindUsbDevice(SerialDeviceInfo deviceInfo)
        {
            var deviceList = _usbManager.DeviceList;
            return deviceList?.Values.FirstOrDefault(d =>
                d.DeviceId.ToString() == deviceInfo.DeviceId ||
                (d.VendorId == deviceInfo.VendorId && d.ProductId == deviceInfo.ProductId));
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONECTAR
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> ConectarAsync(SerialDeviceInfo deviceInfo, SerialConfig config)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("[Serial] Já está conectado");
                        return true;
                    }
                }

                RaiseConnectionStatus(ConnectionStatus.Connecting, deviceInfo.DisplayName);

                // Encontrar dispositivo
                var device = FindUsbDevice(deviceInfo);
                if (device == null)
                {
                    RaiseError("Dispositivo não encontrado", null, true);
                    return false;
                }

                // Verificar permissão
                if (!_usbManager.HasPermission(device))
                {
                    var granted = await SolicitarPermissaoAsync(deviceInfo);
                    if (!granted)
                    {
                        RaiseError("Permissão USB negada", null, true);
                        return false;
                    }
                }

                // Obter driver
                var prober = UsbSerialProber.DefaultProber;
                var driver = prober.ProbeDevice(device);

                if (driver == null)
                {
                    RaiseError("Driver não encontrado para este dispositivo", null, true);
                    return false;
                }

                if (driver.Ports.Count == 0)
                {
                    RaiseError("Nenhuma porta serial disponível", null, true);
                    return false;
                }

                // Abrir conexão USB
                _connection = _usbManager.OpenDevice(device);
                if (_connection == null)
                {
                    RaiseError("Falha ao abrir conexão USB", null, true);
                    return false;
                }

                // Obter e abrir porta serial
                _serialPort = driver.Ports[0];
                _serialPort.Open(_connection);

                // Configurar parâmetros
                var stopBits = config.StopBits switch
                {
                    StopBits.One => UsbSerialPort.Stopbits1,
                    StopBits.OnePointFive => UsbSerialPort.Stopbits15,
                    StopBits.Two => UsbSerialPort.Stopbits2,
                    _ => UsbSerialPort.Stopbits1
                };

                var parity = config.Parity switch
                {
                    Parity.None => UsbSerialPort.ParityNone,
                    Parity.Even => UsbSerialPort.ParityEven,
                    Parity.Odd => UsbSerialPort.ParityOdd,
                    Parity.Mark => UsbSerialPort.ParityMark,
                    Parity.Space => UsbSerialPort.ParitySpace,
                    _ => UsbSerialPort.ParityNone
                };

                _serialPort.SetParameters(config.BaudRate, config.DataBits, stopBits, parity);

                // Configurar DTR/RTS se suportado
                try
                {
                    _serialPort.DTR = true;
                    _serialPort.RTS = true;
                }
                catch
                {
                    // Alguns dispositivos não suportam
                }

                // Salvar estado
                _currentDevice = device;
                _currentDeviceInfo = deviceInfo;
                _currentConfig = config;
                _lineEnding = config.LineEnding;
                _reconnectAttempts = 0;

                lock (_lockObject)
                {
                    _isConnected = true;
                }

                // Iniciar leitura
                IniciarLeitura();

                // Iniciar watchdog
                IniciarWatchdog();

                // Resetar timestamp de leitura
                _ultimaLeitura = DateTime.UtcNow;

                System.Diagnostics.Debug.WriteLine(
                    $"[Serial] Conectado: {deviceInfo.DisplayName} @ {config.BaudRate} baud");

                RaiseConnectionStatus(ConnectionStatus.Connected, deviceInfo.DisplayName);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Serial] Erro ao conectar: {ex}");
                RaiseError($"Erro ao conectar: {ex.Message}", ex, true);

                await DesconectarAsync();
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // DESCONECTAR
        // ═══════════════════════════════════════════════════════════════════

        public async Task DesconectarAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[Serial] Desconectando...");
                
                // Parar watchdog
                PararWatchdog();

                // Parar leitura
                PararLeitura();

                // Fechar porta
                if (_serialPort != null)
                {
                    try
                    {
                        _serialPort.Close();
                    }
                    catch { }
                    _serialPort = null;
                }

                // Fechar conexão
                if (_connection != null)
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch { }
                    _connection = null;
                }

                _currentDevice = null;

                lock (_lockObject)
                {
                    _isConnected = false;
                }

                _lineBuffer.Clear();

                RaiseConnectionStatus(ConnectionStatus.Disconnected, null);

                System.Diagnostics.Debug.WriteLine("[Serial] Desconectado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Serial] Erro ao desconectar: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════════════
        // LEITURA DE DADOS
        // ═══════════════════════════════════════════════════════════════════

        private void IniciarLeitura()
        {
            _readCancellationToken = new CancellationTokenSource();
            _readTask = Task.Run(() => LoopLeituraAsync(_readCancellationToken.Token));
        }

        private void PararLeitura()
        {
            _readCancellationToken?.Cancel();

            try
            {
                _readTask?.Wait(2000);
            }
            catch { }

            _readCancellationToken?.Dispose();
            _readCancellationToken = null;
            _readTask = null;
        }

        private async Task LoopLeituraAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[READ_BUFFER_SIZE];
            var errosConsecutivos = 0;
            const int MAX_ERROS_CONSECUTIVOS = 5;

            System.Diagnostics.Debug.WriteLine("[Serial] Loop de leitura iniciado");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_serialPort == null || !_isConnected)
                    {
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }

                    int bytesRead;

                    try
                    {
                        bytesRead = _serialPort.Read(buffer, READ_TIMEOUT_MS);
                    }
                    catch (Java.IO.IOException ex)
                    {
                        // IOException geralmente indica problema de conexão
                        errosConsecutivos++;

                        System.Diagnostics.Debug.WriteLine(
                            $"[Serial] IOException na leitura ({errosConsecutivos}/{MAX_ERROS_CONSECUTIVOS}): {ex.Message}");

                        if (errosConsecutivos >= MAX_ERROS_CONSECUTIVOS)
                        {
                            System.Diagnostics.Debug.WriteLine("[Serial] Muitos erros consecutivos, tentando reconectar...");
                            await TratarDesconexaoAsync();
                            errosConsecutivos = 0;
                        }
                        else
                        {
                            await Task.Delay(200, cancellationToken);
                        }

                        continue;
                    }

                    // Reset contador de erros após leitura bem-sucedida
                    errosConsecutivos = 0;

                    if (bytesRead > 0)
                    {
                        string dados = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        _lineBuffer.Append(dados);
                        ProcessarBuffer();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Porta foi fechada
                    System.Diagnostics.Debug.WriteLine("[Serial] Porta foi fechada durante leitura");
                    break;
                }
                catch (Exception ex)
                {
                    errosConsecutivos++;

                    System.Diagnostics.Debug.WriteLine(
                        $"[Serial] Erro não esperado na leitura: {ex.GetType().Name} - {ex.Message}");

                    RaiseError($"Erro na leitura: {ex.Message}", ex, false);

                    if (errosConsecutivos >= MAX_ERROS_CONSECUTIVOS)
                    {
                        System.Diagnostics.Debug.WriteLine("[Serial] Muitos erros, encerrando loop...");
                        await TratarDesconexaoAsync();
                        break;
                    }

                    await Task.Delay(500, cancellationToken);
                }
            }

            System.Diagnostics.Debug.WriteLine("[Serial] Loop de leitura finalizado");
        }

        /// <summary>
        /// Inicia o watchdog que monitora a conexão
        /// </summary>
        private void IniciarWatchdog()
        {
            PararWatchdog();

            _watchdogTimer = new Timer(WatchdogCallback, null, WATCHDOG_INTERVAL_MS, WATCHDOG_INTERVAL_MS);

            System.Diagnostics.Debug.WriteLine("[Serial] Watchdog iniciado");
        }

        private void PararWatchdog()
        {
            _watchdogTimer?.Dispose();
            _watchdogTimer = null;
        }

        private async void WatchdogCallback(object? state)
        {
            try
            {
                if (!_isConnected || _isReconnecting)
                    return;

                // Verificar se está recebendo dados
                var tempoSemLeitura = (DateTime.UtcNow - _ultimaLeitura).TotalMilliseconds;

                if (_ultimaLeitura != DateTime.MinValue && tempoSemLeitura > WATCHDOG_TIMEOUT_MS)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Serial] Watchdog: Sem leituras há {tempoSemLeitura / 1000:F1}s, verificando conexão...");

                    // Tentar verificar se a porta ainda está funcional
                    var portaOk = await VerificarPortaAsync();

                    if (!portaOk)
                    {
                        System.Diagnostics.Debug.WriteLine("[Serial] Watchdog: Porta com problemas, reconectando...");
                        await TratarDesconexaoAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Serial] Watchdog erro: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica se a porta serial ainda está funcional
        /// </summary>
        private async Task<bool> VerificarPortaAsync()
        {
            try
            {
                if (_serialPort == null || _connection == null)
                    return false;

                // Tentar verificar sinais de controle (se suportado)
                try
                {
                    var cd = _serialPort.CD;
                    var cts = _serialPort.CTS;
                    // Se conseguiu ler, porta está OK
                    return true;
                }
                catch
                {
                    // Alguns drivers não suportam, não é erro
                }

                // Tentar uma leitura rápida
                var buffer = new byte[1];
                try
                {
                    // Leitura com timeout curto
                    _ = _serialPort.Read(buffer, 50);
                    return true;
                }
                catch (Java.IO.IOException)
                {
                    // IOException indica problema de conexão
                    return false;
                }
                catch
                {
                    // Outros erros podem ser timeout normal
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ProcessarBuffer()
        {
            var bufferStr = _lineBuffer.ToString();

            int index;
            while ((index = bufferStr.IndexOf(_lineEnding, StringComparison.Ordinal)) >= 0)
            {
                var linha = bufferStr.Substring(0, index).Trim();
                bufferStr = bufferStr.Substring(index + _lineEnding.Length);

                if (!string.IsNullOrEmpty(linha) && IsValidLine(linha))
                {
                    // REGISTRAR TIMESTAMP DA ÚLTIMA LEITURA
                    _ultimaLeitura = DateTime.UtcNow;

                    System.Diagnostics.Debug.WriteLine($"[Serial] Linha recebida: {linha}");

                    var eventArgs = new SerialDataEventArgs
                    {
                        Data = linha,
                        RawData = Encoding.ASCII.GetBytes(linha),
                        Timestamp = DateTime.UtcNow
                    };

                    DataReceived?.Invoke(this, eventArgs);
                }
            }

            _lineBuffer.Clear();
            _lineBuffer.Append(bufferStr);

            if (_lineBuffer.Length > 10000)
            {
                _lineBuffer.Clear();
            }
        }

        /// <summary>
        /// Verifica se a linha contém apenas caracteres válidos
        /// </summary>
        private bool IsValidLine(string linha)
        {
            if (string.IsNullOrWhiteSpace(linha))
                return false;

            // Verificar se tem pelo menos um dígito (deve ter número da moto)
            if (!linha.Any(char.IsDigit))
                return false;

            // Verificar caracteres válidos (alfanuméricos, pontuação comum)
            var caracteresValidos = linha.All(c =>
                char.IsLetterOrDigit(c) ||
                c == ',' || c == ':' || c == '.' || c == '-' || c == '_' ||
                c == '#' || c == '@' || c == ' ' || c == '/' || c == '\\');

            if (!caracteresValidos)
                return false;

            // Verificar tamanho razoável
            if (linha.Length > 200)
                return false;

            return true;
        }

        /// <summary>
        /// Escapa caracteres especiais para debug
        /// </summary>
        private string EscapeString(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if (c < 32 || c > 126)
                    sb.Append($"\\x{(int)c:X2}");
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENVIAR DADOS
        // ═══════════════════════════════════════════════════════════════════

        public Task EnviarAsync(string dados)
        {
            return EnviarAsync(Encoding.ASCII.GetBytes(dados));
        }

        public Task EnviarAsync(byte[] dados)
        {
            try
            {
                if (_serialPort == null || !_isConnected)
                {
                    throw new InvalidOperationException("Não está conectado");
                }

                _serialPort.Write(dados, 1000);

                System.Diagnostics.Debug.WriteLine($"[Serial] Enviado: {Encoding.ASCII.GetString(dados)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Serial] Erro ao enviar: {ex.Message}");
                RaiseError($"Erro ao enviar dados: {ex.Message}", ex, false);
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════════════
        // RECONEXÃO AUTOMÁTICA
        // ═══════════════════════════════════════════════════════════════════

        private async Task TratarDesconexaoAsync()
        {
            if (_isReconnecting) return;

            _isReconnecting = true;

            try
            {
                lock (_lockObject)
                {
                    _isConnected = false;
                }

                // Parar watchdog durante reconexão
                PararWatchdog();

                RaiseConnectionStatus(ConnectionStatus.Reconnecting, _currentDeviceInfo?.DisplayName);

                // Fechar conexão atual
                await FecharConexaoAtualAsync();

                // Tentar reconectar com backoff exponencial
                while (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS &&
                       _currentDeviceInfo != null &&
                       _currentConfig != null &&
                       !_disposed)
                {
                    _reconnectAttempts++;

                    // Calcular delay com backoff exponencial
                    // 2s, 4s, 8s, 16s, 32s
                    var delay = Math.Min(RECONNECT_DELAY_MS * (int)Math.Pow(2, _reconnectAttempts - 1), 32000);

                    System.Diagnostics.Debug.WriteLine(
                        $"[Serial] Tentativa de reconexão {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS} " +
                        $"em {delay / 1000}s...");

                    RaiseConnectionStatus(ConnectionStatus.Reconnecting, _currentDeviceInfo?.DisplayName,
                        $"Tentativa {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}...");

                    await Task.Delay(delay);

                    // Verificar se dispositivo ainda está conectado
                    var devices = await ListarDispositivosAsync();
                    var currentDevice = devices.FirstOrDefault(d =>
                        d.VendorId == _currentDeviceInfo.VendorId &&
                        d.ProductId == _currentDeviceInfo.ProductId);

                    if (currentDevice == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[Serial] Dispositivo não está mais conectado");
                        continue;
                    }

                    // Tentar conectar novamente
                    try
                    {
                        var success = await ConectarInternoAsync(currentDevice, _currentConfig);

                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine("[Serial] Reconectado com sucesso!");
                            _reconnectAttempts = 0;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Serial] Falha na tentativa de reconexão: {ex.Message}");
                    }
                }

                // Falha definitiva
                System.Diagnostics.Debug.WriteLine("[Serial] Falha na reconexão após múltiplas tentativas");

                RaiseError(
                    "Conexão perdida. Verifique o cabo USB e tente reconectar manualmente.",
                    null,
                    true);

                RaiseConnectionStatus(ConnectionStatus.Error, null,
                    "Falha na reconexão automática");

                // Limpar estado
                _currentDevice = null;
                _currentDeviceInfo = null;
                _currentConfig = null;
                _reconnectAttempts = 0;
            }
            finally
            {
                _isReconnecting = false;
            }
        }
        // ═══════════════════════════════════════════════════════════════════
        // EVENTOS USB
        // ═══════════════════════════════════════════════════════════════════

        private async Task FecharConexaoAtualAsync()
        {
            try
            {
                PararLeitura();
            }
            catch { }

            try
            {
                _serialPort?.Close();
            }
            catch { }

            try
            {
                _connection?.Close();
            }
            catch { }

            _serialPort = null;
            _connection = null;

            await Task.Delay(500); // Aguardar recursos serem liberados
        }

        /// <summary>
        /// Conexão interna (sem reiniciar watchdog/leitura - usado pela reconexão)
        /// </summary>
        private async Task<bool> ConectarInternoAsync(SerialDeviceInfo deviceInfo, SerialConfig config)
        {
            // Encontrar dispositivo
            var device = FindUsbDevice(deviceInfo);
            if (device == null)
                return false;

            // Verificar permissão
            if (!_usbManager.HasPermission(device))
                return false;

            // Obter driver
            var prober = UsbSerialProber.DefaultProber;
            var driver = prober.ProbeDevice(device);

            if (driver == null || driver.Ports.Count == 0)
                return false;

            // Abrir conexão USB
            _connection = _usbManager.OpenDevice(device);
            if (_connection == null)
                return false;

            // Abrir porta serial
            _serialPort = driver.Ports[0];
            _serialPort.Open(_connection);

            // Configurar parâmetros
            var stopBits = config.StopBits switch
            {
                StopBits.One => UsbSerialPort.Stopbits1,
                StopBits.OnePointFive => UsbSerialPort.Stopbits15,
                StopBits.Two => UsbSerialPort.Stopbits2,
                _ => UsbSerialPort.Stopbits1
            };

            var parity = config.Parity switch
            {
                Parity.None => UsbSerialPort.ParityNone,
                Parity.Even => UsbSerialPort.ParityEven,
                Parity.Odd => UsbSerialPort.ParityOdd,
                Parity.Mark => UsbSerialPort.ParityMark,
                Parity.Space => UsbSerialPort.ParitySpace,
                _ => UsbSerialPort.ParityNone
            };

            _serialPort.SetParameters(config.BaudRate, config.DataBits, stopBits, parity);

            try
            {
                _serialPort.DTR = true;
                _serialPort.RTS = true;
            }
            catch { }

            // Atualizar estado
            _currentDevice = device;
            _currentDeviceInfo = deviceInfo;
            _currentConfig = config;
            _lineBuffer.Clear();
            _ultimaLeitura = DateTime.UtcNow;

            lock (_lockObject)
            {
                _isConnected = true;
            }

            // Iniciar leitura e watchdog
            IniciarLeitura();
            IniciarWatchdog();

            RaiseConnectionStatus(ConnectionStatus.Connected, deviceInfo.DisplayName);

            return true;
        }

        private async void OnUsbDeviceAttached(object? sender, UsbDevice? device)
        {
            if (device == null) return;

            System.Diagnostics.Debug.WriteLine($"[Serial] Dispositivo USB conectado: {device.DeviceName}");

            // Se estávamos tentando reconectar ao mesmo dispositivo
            if (_currentDeviceInfo != null &&
                device.VendorId == _currentDeviceInfo.VendorId &&
                device.ProductId == _currentDeviceInfo.ProductId &&
                !_isConnected)
            {
                System.Diagnostics.Debug.WriteLine("[Serial] Dispositivo reconectado, tentando reconectar...");

                // Pequeno delay para o dispositivo se estabilizar
                await Task.Delay(1000);

                var devices = await ListarDispositivosAsync();
                var deviceInfo = devices.FirstOrDefault(d =>
                    d.VendorId == _currentDeviceInfo.VendorId &&
                    d.ProductId == _currentDeviceInfo.ProductId);

                if (deviceInfo != null && _currentConfig != null)
                {
                    await ConectarAsync(deviceInfo, _currentConfig);
                }
            }
        }

        private async void OnUsbDeviceDetached(object? sender, UsbDevice? device)
        {
            if (device == null) return;

            System.Diagnostics.Debug.WriteLine($"[Serial] Dispositivo USB desconectado: {device.DeviceName}");

            // Se é o dispositivo atual
            if (_currentDevice != null && device.DeviceId == _currentDevice.DeviceId)
            {
                System.Diagnostics.Debug.WriteLine("[Serial] Dispositivo atual foi desconectado");

                lock (_lockObject)
                {
                    _isConnected = false;
                }

                RaiseConnectionStatus(ConnectionStatus.Disconnected, _currentDeviceInfo?.DisplayName,
                    "Dispositivo USB desconectado");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private void RaiseConnectionStatus(ConnectionStatus status, string? deviceName, string? message = null)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
            {
                Status = status,
                DeviceName = deviceName,
                Message = message
            });
        }

        private void RaiseError(string message, Exception? exception, bool isFatal)
        {
            ErrorOccurred?.Invoke(this, new SerialErrorEventArgs
            {
                Message = message,
                Exception = exception,
                IsFatal = isFatal
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            MainActivity.UsbDeviceAttached -= OnUsbDeviceAttached;
            MainActivity.UsbDeviceDetached -= OnUsbDeviceDetached;

            _ = DesconectarAsync();

            GC.SuppressFinalize(this);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RECEIVER DE PERMISSÃO USB
    // ═══════════════════════════════════════════════════════════════════════

    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class UsbPermissionReceiver : BroadcastReceiver
    {
        private readonly Action<bool>? _callback;

        public UsbPermissionReceiver() { }

        public UsbPermissionReceiver(Action<bool> callback)
        {
            _callback = callback;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == "com.cronometragem.coletor.USB_PERMISSION")
            {
                var granted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);
                _callback?.Invoke(granted);
            }
        }
    }


}