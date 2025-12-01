using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class LeituraDto
{
    /// <summary>
    /// Número da moto lido/digitado
    /// </summary>
    [Required(ErrorMessage = "Número da moto é obrigatório")]
    [Range(1, 9999, ErrorMessage = "Número da moto deve estar entre 1 e 9999")]
    public int NumeroMoto { get; set; }

    /// <summary>
    /// Timestamp exato da leitura no dispositivo
    /// Formato ISO 8601: "2025-01-15T10:30:45.123Z"
    /// </summary>
    [Required(ErrorMessage = "Timestamp é obrigatório")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Tipo de leitura: E (Entrada), S (Saída), P (Passagem)
    /// </summary>
    [Required(ErrorMessage = "Tipo é obrigatório")]
    [RegularExpression("^[ESP]$", ErrorMessage = "Tipo deve ser E, S ou P")]
    public string Tipo { get; set; }

    /// <summary>
    /// ID da especial (obrigatório para ENDURO)
    /// </summary>
    public int? IdEspecial { get; set; }

    /// <summary>
    /// Número da volta
    /// </summary>
    [Range(1, 99)]
    public int Volta { get; set; } = 1;

    /// <summary>
    /// ID da etapa
    /// </summary>
    [Required(ErrorMessage = "ID da etapa é obrigatório")]
    public int IdEtapa { get; set; }

    /// <summary>
    /// Device ID do coletor (identificação única)
    /// </summary>
    [Required(ErrorMessage = "DeviceId é obrigatório")]
    [MaxLength(100)]
    public string DeviceId { get; set; }

    /// <summary>
    /// Dados brutos da leitura (opcional, para debug)
    /// </summary>
    [MaxLength(500)]
    public string? DadosBrutos { get; set; }

    /// <summary>
    /// ID único da leitura no dispositivo (para evitar duplicatas)
    /// </summary>
    [MaxLength(50)]
    public string? IdLocal { get; set; }
}
