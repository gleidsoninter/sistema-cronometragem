using System.Text.Json.Serialization;

namespace AppColetor.Models.DTOs
{
    /// <summary>
    /// Requisição de autenticação do dispositivo
    /// </summary>
    public class AuthRequestDto
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "";

        [JsonPropertyName("senha")]
        public string Senha { get; set; } = "";

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "COLETOR";
    }

    /// <summary>
    /// Resultado da autenticação
    /// </summary>
    public class AuthResultDto
    {
        [JsonPropertyName("sucesso")]
        public bool Sucesso { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expiraEm")]
        public DateTime? ExpiraEm { get; set; }

        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("nome")]
        public string? Nome { get; set; }

        [JsonPropertyName("mensagem")]
        public string? Mensagem { get; set; }
    }

    /// <summary>
    /// Informações da etapa
    /// </summary>
    public class EtapaInfoDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = "";

        [JsonPropertyName("nomeEvento")]
        public string NomeEvento { get; set; } = "";

        [JsonPropertyName("dataHora")]
        public DateTime DataHora { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("tipoCronometragem")]
        public string TipoCronometragem { get; set; } = "";

        [JsonPropertyName("numeroVoltas")]
        public int? NumeroVoltas { get; set; }

        [JsonPropertyName("numeroEspeciais")]
        public int? NumeroEspeciais { get; set; }

        [JsonPropertyName("totalInscritos")]
        public int TotalInscritos { get; set; }
    }

    /// <summary>
    /// Heartbeat do coletor
    /// </summary>
    public class HeartbeatDto
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "ATIVO";

        [JsonPropertyName("bateria")]
        public int? Bateria { get; set; }

        [JsonPropertyName("leiturasNaoSincronizadas")]
        public int LeiturasNaoSincronizadas { get; set; }
    }

    /// <summary>
    /// Erro da API
    /// </summary>
    public class ApiErrorDto
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("mensagem")]
        public string Mensagem { get; set; } = "";

        [JsonPropertyName("detalhes")]
        public string? Detalhes { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}