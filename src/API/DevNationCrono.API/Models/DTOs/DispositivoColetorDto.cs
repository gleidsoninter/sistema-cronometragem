using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class DispositivoColetorDto
{
    public int Id { get; set; }
    public int IdEvento { get; set; }
    public string NomeEvento { get; set; }
    public int IdEtapa { get; set; }
    public string NomeEtapa { get; set; }
    public string Nome { get; set; }
    public string Tipo { get; set; }
    public string TipoDescricao { get; set; }
    public int? IdEspecial { get; set; }
    public string DeviceId { get; set; }
    public string StatusConexao { get; set; }
    public DateTime? UltimaConexao { get; set; }
    public DateTime? UltimaLeitura { get; set; }
    public int TotalLeituras { get; set; }
    public bool Ativo { get; set; }
}

public class DispositivoColetorCreateDto
{
    [Required]
    public int IdEvento { get; set; }

    [Required]
    public int IdEtapa { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    [Required]
    [RegularExpression("^[ESP]$", ErrorMessage = "Tipo deve ser E, S ou P")]
    public string Tipo { get; set; }

    public int? IdEspecial { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; }
}

public class DispositivoColetorUpdateDto
{
    [MaxLength(100)]
    public string? Nome { get; set; }

    public int? IdEspecial { get; set; }

    public bool? Ativo { get; set; }
}

// ===== AUTENTICAÇÃO DO COLETOR =====
public class ColetorLoginDto
{
    [Required]
    public string DeviceId { get; set; }

    [Required]
    public int IdEtapa { get; set; }
}

public class ColetorLoginResponseDto
{
    public bool Sucesso { get; set; }
    public string Token { get; set; }
    public int IdDispositivo { get; set; }
    public string Nome { get; set; }
    public string Tipo { get; set; }
    public int? IdEspecial { get; set; }
    public string NomeEvento { get; set; }
    public string NomeEtapa { get; set; }
    public string TipoCronometragem { get; set; }
    public string Mensagem { get; set; }
}

// ===== STATUS/HEARTBEAT =====
public class ColetorHeartbeatDto
{
    [Required]
    public string DeviceId { get; set; }

    public int LeiturasPendentes { get; set; }

    public DateTime UltimaLeituraLocal { get; set; }

    public int NivelBateria { get; set; }

    public bool ConexaoInternet { get; set; }
}
