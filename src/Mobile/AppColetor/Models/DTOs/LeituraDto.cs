using System.Text.Json.Serialization;

namespace AppColetor.Models.DTOs
{
    /// <summary>
    /// DTO para envio de leitura para a API
    /// </summary>
    public class LeituraDto
    {
        [JsonPropertyName("numeroMoto")]
        public int NumeroMoto { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "P";

        [JsonPropertyName("idEtapa")]
        public int IdEtapa { get; set; }

        [JsonPropertyName("volta")]
        public int? Volta { get; set; }

        [JsonPropertyName("idEspecial")]
        public int? IdEspecial { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "";

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = "";
    }

    /// <summary>
    /// DTO para envio de lote de leituras
    /// </summary>
    public class LoteLeituraDto
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "";

        [JsonPropertyName("idEtapa")]
        public int IdEtapa { get; set; }

        [JsonPropertyName("leituras")]
        public List<LeituraItemDto> Leituras { get; set; } = new();
    }

    public class LeituraItemDto
    {
        [JsonPropertyName("numeroMoto")]
        public int NumeroMoto { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "P";

        [JsonPropertyName("volta")]
        public int? Volta { get; set; }

        [JsonPropertyName("idEspecial")]
        public int? IdEspecial { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = "";
    }

    /// <summary>
    /// Resposta da API para uma leitura
    /// </summary>
    public class LeituraResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("mensagem")]
        public string? Mensagem { get; set; }

        [JsonPropertyName("numeroMoto")]
        public int NumeroMoto { get; set; }

        [JsonPropertyName("volta")]
        public int? Volta { get; set; }

        [JsonPropertyName("tempoCalculadoSegundos")]
        public decimal? TempoCalculadoSegundos { get; set; }

        [JsonPropertyName("tempoFormatado")]
        public string? TempoFormatado { get; set; }

        [JsonPropertyName("posicaoAtual")]
        public int? PosicaoAtual { get; set; }
    }

    /// <summary>
    /// Resposta da API para lote de leituras
    /// </summary>
    public class LoteResponseDto
    {
        [JsonPropertyName("totalRecebidas")]
        public int TotalRecebidas { get; set; }

        [JsonPropertyName("totalProcessadas")]
        public int TotalProcessadas { get; set; }

        [JsonPropertyName("totalDuplicadas")]
        public int TotalDuplicadas { get; set; }

        [JsonPropertyName("totalErros")]
        public int TotalErros { get; set; }

        [JsonPropertyName("detalhes")]
        public List<LeituraResponseDto>? Detalhes { get; set; }
    }
}