using System.Text.Json.Serialization;

namespace DevNationCrono.API.Models.DTOs;

public class RegistroDispositivoDto
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "PASSAGEM";

    [JsonPropertyName("idEspecial")]
    public int? IdEspecial { get; set; }

    [JsonPropertyName("idEtapa")]
    public int IdEtapa { get; set; }

    [JsonPropertyName("androidId")]
    public string? AndroidId { get; set; }

    [JsonPropertyName("modeloDispositivo")]
    public string? ModeloDispositivo { get; set; }

    [JsonPropertyName("fabricante")]
    public string? Fabricante { get; set; }

    [JsonPropertyName("versaoApp")]
    public string? VersaoApp { get; set; }

    [JsonPropertyName("senha")]
    public string Senha { get; set; } = "";
}

public class RegistroDispositivoResultDto
{
    [JsonPropertyName("sucesso")]
    public bool Sucesso { get; set; }

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = "";

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expiraEm")]
    public DateTime? ExpiraEm { get; set; }

    [JsonPropertyName("configServidor")]
    public ConfigServidorDto? ConfigServidor { get; set; }
}

public class ConfigServidorDto
{
    [JsonPropertyName("intervaloHeartbeatSegundos")]
    public int IntervaloHeartbeatSegundos { get; set; } = 30;

    [JsonPropertyName("intervaloSyncSegundos")]
    public int IntervaloSyncSegundos { get; set; } = 5;

    [JsonPropertyName("tamanhoLoteSync")]
    public int TamanhoLoteSync { get; set; } = 50;

    [JsonPropertyName("ntpServers")]
    public List<string> NtpServers { get; set; } = new();
}

public class HeartbeatDto
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("idEspecial")]
    public int? IdEspecial { get; set; }

    [JsonPropertyName("idEtapa")]
    public int IdEtapa { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("nivelBateria")]
    public int NivelBateria { get; set; }

    [JsonPropertyName("leiturasPendentes")]
    public int LeiturasPendentes { get; set; }

    [JsonPropertyName("totalLeiturasSessao")]
    public int TotalLeiturasSessao { get; set; }

    [JsonPropertyName("versaoApp")]
    public string? VersaoApp { get; set; }

    [JsonPropertyName("modeloDispositivo")]
    public string? ModeloDispositivo { get; set; }

    [JsonPropertyName("offsetTempoMs")]
    public long OffsetTempoMs { get; set; }
}

public class HeartbeatResponseDto
{
    [JsonPropertyName("sucesso")]
    public bool Sucesso { get; set; }

    [JsonPropertyName("timestampServidor")]
    public DateTime TimestampServidor { get; set; }

    [JsonPropertyName("comandos")]
    public List<ComandoDispositivoDto>? Comandos { get; set; }
}

public class ComandoDispositivoDto
{
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    [JsonPropertyName("parametros")]
    public Dictionary<string, object>? Parametros { get; set; }
}

public class TempoServidorDto
{
    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; }

    [JsonPropertyName("timestampUnixMs")]
    public long TimestampUnixMs { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "UTC";
}

public class DispositivoStatusDto
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    [JsonPropertyName("idEspecial")]
    public int? IdEspecial { get; set; }

    [JsonPropertyName("nomeEspecial")]
    public string? NomeEspecial { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("online")]
    public bool Online { get; set; }

    [JsonPropertyName("nivelBateria")]
    public int NivelBateria { get; set; }

    [JsonPropertyName("leiturasPendentes")]
    public int LeiturasPendentes { get; set; }

    [JsonPropertyName("totalLeiturasSessao")]
    public int TotalLeiturasSessao { get; set; }

    [JsonPropertyName("ultimoHeartbeat")]
    public DateTime? UltimoHeartbeat { get; set; }

    [JsonPropertyName("ultimaSync")]
    public DateTime? UltimaSync { get; set; }
}

public class EstatisticasDispositivosDto
{
    [JsonPropertyName("totalRegistrados")]
    public int TotalRegistrados { get; set; }

    [JsonPropertyName("totalOnline")]
    public int TotalOnline { get; set; }

    [JsonPropertyName("totalOffline")]
    public int TotalOffline { get; set; }

    [JsonPropertyName("totalLeiturasPendentes")]
    public int TotalLeiturasPendentes { get; set; }

    [JsonPropertyName("porTipo")]
    public Dictionary<string, int> PorTipo { get; set; } = new();

    [JsonPropertyName("alertas")]
    public List<AlertaDispositivoDto> Alertas { get; set; } = new();
}

public class AlertaDispositivoDto
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = "";

    [JsonPropertyName("severidade")]
    public string Severidade { get; set; } = "WARNING";
}