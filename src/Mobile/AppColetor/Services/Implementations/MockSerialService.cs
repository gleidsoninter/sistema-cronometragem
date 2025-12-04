using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    /// <summary>
    /// Serviço mock para testes sem hardware USB
    /// </summary>
    public class MockSerialService : ISerialService
    {
        private Timer? _timer;
        private bool _isConnected;
        private readonly Random _random = new();
        private int _sequencia;

        public event EventHandler<SerialDataEventArgs>? DataReceived;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<SerialErrorEventArgs>? ErrorOccurred;

        public bool IsConnected => _isConnected;
        public string? DeviceName => _isConnected ? "Simulador USB" : null;

        public Task<List<SerialDeviceInfo>> ListarDispositivosAsync()
        {
            var dispositivos = new List<SerialDeviceInfo>
            {
                new SerialDeviceInfo
                {
                    DeviceId = "MOCK-001",
                    Name = "Simulador de Coletor",
                    VendorId = 0x1A86,
                    ProductId = 0x7523,
                    ChipType = "MOCK",
                    HasPermission = true
                }
            };

            return Task.FromResult(dispositivos);
        }

        public Task<bool> SolicitarPermissaoAsync(SerialDeviceInfo device)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ConectarAsync(SerialDeviceInfo device, SerialConfig config)
        {
            _isConnected = true;
            _sequencia = 0;

            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
            {
                Status = ConnectionStatus.Connected,
                DeviceName = device.DisplayName
            });

            // Iniciar simulação de leituras
            _timer = new Timer(SimularLeitura, null, 2000, GetIntervaloAleatorio());

            return Task.FromResult(true);
        }

        public Task DesconectarAsync()
        {
            _timer?.Dispose();
            _timer = null;
            _isConnected = false;

            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
            {
                Status = ConnectionStatus.Disconnected
            });

            return Task.CompletedTask;
        }

        public Task EnviarAsync(string dados) => Task.CompletedTask;
        public Task EnviarAsync(byte[] dados) => Task.CompletedTask;

        private void SimularLeitura(object? state)
        {
            if (!_isConnected) return;

            _sequencia++;

            // Simular diferentes formatos
            var formato = _sequencia % 3;
            string dados = formato switch
            {
                0 => GerarDadoGenerico(),
                1 => GerarDadoRfTiming(),
                _ => GerarDadoAmb()
            };

            DataReceived?.Invoke(this, new SerialDataEventArgs
            {
                Data = dados,
                RawData = System.Text.Encoding.ASCII.GetBytes(dados),
                Timestamp = DateTime.UtcNow
            });

            // Atualizar intervalo para próxima leitura
            _timer?.Change(GetIntervaloAleatorio(), Timeout.Infinite);
        }

        private string GerarDadoGenerico()
        {
            var moto = _random.Next(1, 100);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return $"{moto},{timestamp}";
        }

        private string GerarDadoRfTiming()
        {
            var moto = _random.Next(1, 100);
            var tempo = DateTime.Now.ToString("HH:mm:ss.fff");
            var volta = _random.Next(1, 20);
            return $"#{moto:D3}:{tempo}:{volta:D3}#";
        }

        private string GerarDadoAmb()
        {
            var transponder = _random.Next(1000000, 9999999);
            var loop = _random.Next(1, 3);
            var time = DateTime.Now.ToString("HHmmssfff");
            var hits = _random.Next(1, 10);
            return $"@T:{transponder}:{loop:D2}:{time}:{hits:D3}";
        }

        private int GetIntervaloAleatorio()
        {
            // Intervalo entre 1 e 5 segundos
            return _random.Next(1000, 5000);
        }
    }
}