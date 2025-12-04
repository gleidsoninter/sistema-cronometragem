using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class DataOptimizationService
    {
        private readonly IConnectivityService _connectivityService;

        private long _bytesEnviados;
        private long _bytesRecebidos;
        private DateTime _inicioSessao = DateTime.UtcNow;

        public long BytesEnviados => _bytesEnviados;
        public long BytesRecebidos => _bytesRecebidos;
        public long TotalBytes => _bytesEnviados + _bytesRecebidos;

        public DataOptimizationService(IConnectivityService connectivityService)
        {
            _connectivityService = connectivityService;
        }

        /// <summary>
        /// Obtém configurações de lote otimizadas para o tipo de conexão
        /// </summary>
        public BatchSettings ObterConfiguracoesLote()
        {
            return _connectivityService.TipoAtual switch
            {
                TipoConexao.WiFi => new BatchSettings
                {
                    TamanhoMaximoLote = 100,
                    CompressaoHabilitada = false,
                    IntervaloEntreLotesMs = 100
                },
                TipoConexao.Cellular => new BatchSettings
                {
                    TamanhoMaximoLote = 50,
                    CompressaoHabilitada = true,
                    IntervaloEntreLotesMs = 500
                },
                _ => new BatchSettings
                {
                    TamanhoMaximoLote = 20,
                    CompressaoHabilitada = true,
                    IntervaloEntreLotesMs = 1000
                }
            };
        }

        /// <summary>
        /// Comprime dados para envio
        /// </summary>
        public byte[] Comprimir(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            var comprimido = output.ToArray();

            System.Diagnostics.Debug.WriteLine(
                $"[DataOpt] Compressão: {bytes.Length} → {comprimido.Length} bytes " +
                $"({(1 - (double)comprimido.Length / bytes.Length) * 100:F1}% redução)");

            return comprimido;
        }

        /// <summary>
        /// Otimiza payload de leituras removendo campos desnecessários
        /// </summary>
        public string OtimizarPayloadLeituras(IEnumerable<Leitura> leituras)
        {
            // Converter para formato compacto
            var leiturasCompactas = leituras.Select(l => new
            {
                m = l.NumeroMoto,          // moto
                t = l.Timestamp.Ticks,     // timestamp como ticks (menor que string)
                p = l.Tipo[0],             // tipo como char (P, E, S)
                e = l.IdEtapa,             // etapa
                v = l.Volta,               // volta (nullable)
                h = l.Hash?[..8]           // apenas 8 primeiros chars do hash
            });

            return JsonSerializer.Serialize(leiturasCompactas);
        }

        /// <summary>
        /// Registra dados enviados
        /// </summary>
        public void RegistrarEnvio(long bytes)
        {
            Interlocked.Add(ref _bytesEnviados, bytes);
        }

        /// <summary>
        /// Registra dados recebidos
        /// </summary>
        public void RegistrarRecebimento(long bytes)
        {
            Interlocked.Add(ref _bytesRecebidos, bytes);
        }

        /// <summary>
        /// Obtém estatísticas de uso de dados
        /// </summary>
        public DataUsageStats ObterEstatisticas()
        {
            var duracao = DateTime.UtcNow - _inicioSessao;

            return new DataUsageStats
            {
                BytesEnviados = _bytesEnviados,
                BytesRecebidos = _bytesRecebidos,
                TotalBytes = TotalBytes,
                DuracaoSessao = duracao,
                TaxaMediaBps = duracao.TotalSeconds > 0
                    ? TotalBytes / duracao.TotalSeconds
                    : 0
            };
        }

        /// <summary>
        /// Reseta contadores
        /// </summary>
        public void ResetarContadores()
        {
            _bytesEnviados = 0;
            _bytesRecebidos = 0;
            _inicioSessao = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica se deve adiar sync para economizar dados
        /// </summary>
        public bool DeveAdiarSync(int leiturasPendentes)
        {
            // Se está em WiFi, não adiar
            if (_connectivityService.IsWiFi)
                return false;

            // Se tem muitas pendentes (>50), não adiar
            if (leiturasPendentes > 50)
                return false;

            // Se está em dados móveis com poucas pendentes, aguardar acumular
            if (_connectivityService.IsCellular && leiturasPendentes < 20)
                return true;

            return false;
        }
    }

    public class BatchSettings
    {
        public int TamanhoMaximoLote { get; set; } = 50;
        public bool CompressaoHabilitada { get; set; }
        public int IntervaloEntreLotesMs { get; set; } = 100;
    }

    public class DataUsageStats
    {
        public long BytesEnviados { get; set; }
        public long BytesRecebidos { get; set; }
        public long TotalBytes { get; set; }
        public TimeSpan DuracaoSessao { get; set; }
        public double TaxaMediaBps { get; set; }

        public string TotalFormatado => FormatarBytes(TotalBytes);

        private string FormatarBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1024.0 / 1024.0:F2} MB";
        }
    }
}