using System.Diagnostics;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class TimeSyncService : IDisposable
    {
        private readonly IApiService _apiService;
        private readonly IConfigService _configService;

        private Timer? _syncTimer;
        private long _offsetMs;
        private DateTime _ultimaSync = DateTime.MinValue;
        private bool _sincronizado;
        private readonly object _lockObject = new();

        private const int SYNC_INTERVAL_MS = 300000; // 5 minutos
        private const int MAX_AMOSTRAS = 5;
        private const int TIMEOUT_MS = 5000;

        public event EventHandler<TimeSyncEventArgs>? SyncCompleted;

        public long OffsetMs => _offsetMs;
        public bool Sincronizado => _sincronizado;
        public DateTime UltimaSync => _ultimaSync;

        public TimeSyncService(IApiService apiService, IConfigService configService)
        {
            _apiService = apiService;
            _configService = configService;
        }

        /// <summary>
        /// Inicia sincronização periódica
        /// </summary>
        public void Iniciar()
        {
            // Sincronizar imediatamente
            _ = SincronizarAsync();

            // Timer para sincronização periódica
            _syncTimer = new Timer(
                async _ => await SincronizarAsync(),
                null,
                TimeSpan.FromMilliseconds(SYNC_INTERVAL_MS),
                TimeSpan.FromMilliseconds(SYNC_INTERVAL_MS));

            System.Diagnostics.Debug.WriteLine("[TimeSync] Serviço iniciado");
        }

        /// <summary>
        /// Para sincronização
        /// </summary>
        public void Parar()
        {
            _syncTimer?.Dispose();
            _syncTimer = null;
        }

        /// <summary>
        /// Executa sincronização com o servidor
        /// </summary>
        public async Task<bool> SincronizarAsync()
        {
            var amostras = new List<long>();

            try
            {
                System.Diagnostics.Debug.WriteLine("[TimeSync] Iniciando sincronização...");

                // Coletar múltiplas amostras para maior precisão
                for (int i = 0; i < MAX_AMOSTRAS; i++)
                {
                    var offset = await MedirOffsetAsync();

                    if (offset.HasValue)
                    {
                        amostras.Add(offset.Value);
                    }

                    // Pequeno delay entre amostras
                    if (i < MAX_AMOSTRAS - 1)
                    {
                        await Task.Delay(200);
                    }
                }

                if (amostras.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TimeSync] Nenhuma amostra válida obtida");
                    return false;
                }

                // Calcular offset médio (removendo outliers)
                var offsetFinal = CalcularOffsetFinal(amostras);

                lock (_lockObject)
                {
                    _offsetMs = offsetFinal;
                    _sincronizado = true;
                    _ultimaSync = DateTime.UtcNow;
                }

                // Salvar offset
                await _configService.SetStringAsync("time_offset_ms", offsetFinal.ToString());
                await _configService.SetStringAsync("time_last_sync", _ultimaSync.ToString("O"));

                System.Diagnostics.Debug.WriteLine(
                    $"[TimeSync] Sincronizado! Offset: {offsetFinal}ms ({offsetFinal / 1000.0:F3}s)");

                // Notificar
                SyncCompleted?.Invoke(this, new TimeSyncEventArgs
                {
                    OffsetMs = offsetFinal,
                    Amostras = amostras.Count,
                    Sucesso = true
                });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeSync] Erro: {ex.Message}");

                SyncCompleted?.Invoke(this, new TimeSyncEventArgs
                {
                    Sucesso = false,
                    Erro = ex.Message
                });

                return false;
            }
        }

        private async Task<long?> MedirOffsetAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Obter tempo do servidor
                var tempoServidor = await _apiService.ObterTempoServidorAsync();

                stopwatch.Stop();
                var t4 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (tempoServidor == null)
                    return null;

                var t2 = tempoServidor.TimestampUnixMs;

                // Calcular RTT e offset
                var rtt = t4 - t1;
                var delay = rtt / 2;
                var offset = t2 - t1 - delay;

                System.Diagnostics.Debug.WriteLine(
                    $"[TimeSync] Amostra: RTT={rtt}ms, Delay={delay}ms, Offset={offset}ms");

                return offset;
            }
            catch
            {
                return null;
            }
        }

        private long CalcularOffsetFinal(List<long> amostras)
        {
            if (amostras.Count == 1)
                return amostras[0];

            // Ordenar e remover outliers (primeiro e último)
            var ordenadas = amostras.OrderBy(x => x).ToList();

            if (ordenadas.Count >= 3)
            {
                ordenadas.RemoveAt(0);
                ordenadas.RemoveAt(ordenadas.Count - 1);
            }

            // Retornar média
            return (long)ordenadas.Average();
        }

        /// <summary>
        /// Obtém o timestamp atual corrigido
        /// </summary>
        public DateTime ObterTimestampCorrigido()
        {
            var agora = DateTime.UtcNow;

            if (_sincronizado)
            {
                return agora.AddMilliseconds(_offsetMs);
            }

            return agora;
        }

        /// <summary>
        /// Obtém o timestamp em milissegundos corrigido
        /// </summary>
        public long ObterTimestampMsCorrigido()
        {
            var agora = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (_sincronizado)
            {
                return agora + _offsetMs;
            }

            return agora;
        }

        /// <summary>
        /// Carrega offset salvo anteriormente
        /// </summary>
        public async Task CarregarOffsetSalvoAsync()
        {
            try
            {
                var offsetStr = await _configService.GetStringAsync("time_offset_ms");
                var lastSyncStr = await _configService.GetStringAsync("time_last_sync");

                if (!string.IsNullOrEmpty(offsetStr) && long.TryParse(offsetStr, out var offset))
                {
                    _offsetMs = offset;

                    if (!string.IsNullOrEmpty(lastSyncStr) &&
                        DateTime.TryParse(lastSyncStr, out var lastSync))
                    {
                        _ultimaSync = lastSync;

                        // Considerar sincronizado se última sync foi há menos de 1 hora
                        _sincronizado = (DateTime.UtcNow - lastSync).TotalHours < 1;
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"[TimeSync] Offset carregado: {_offsetMs}ms, Última sync: {_ultimaSync}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeSync] Erro ao carregar offset: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Parar();
        }
    }

    public class TimeSyncEventArgs : EventArgs
    {
        public bool Sucesso { get; set; }
        public long OffsetMs { get; set; }
        public int Amostras { get; set; }
        public string? Erro { get; set; }
    }
}