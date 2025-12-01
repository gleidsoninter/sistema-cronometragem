using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("Tempos")]
public class Tempo
{
    [Key]
    public long Id { get; set; } // BIGINT para muitas leituras

    [Required]
    public int IdEtapa { get; set; }

    public int? IdInscricao { get; set; } // Pode ser null se moto não inscrita

    [Required]
    public int NumeroMoto { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } // Momento exato da leitura

    [Required]
    [MaxLength(1)]
    public string Tipo { get; set; } // E, S, P

    public int? IdEspecial { get; set; } // Qual especial (1, 2, 3...)

    public int Volta { get; set; } = 1;

    public int IdDispositivo { get; set; } // Qual coletor enviou

    /// <summary>
    /// Tempo calculado em segundos (para especiais ENDURO)
    /// Tempo da volta em segundos (para CIRCUITO)
    /// </summary>
    [Column(TypeName = "decimal(10,3)")]
    public decimal? TempoCalculadoSegundos { get; set; }

    /// <summary>
    /// Tempo formatado (ex: "00:05:23.456")
    /// </summary>
    [MaxLength(20)]
    public string? TempoFormatado { get; set; }

    /// <summary>
    /// Se é a melhor volta do piloto
    /// </summary>
    public bool MelhorVolta { get; set; } = false;

    /// <summary>
    /// Se esta leitura foi sincronizada (veio de fila offline)
    /// </summary>
    public bool Sincronizado { get; set; } = true;

    /// <summary>
    /// Se foi corrigido manualmente pelo organizador
    /// </summary>
    public bool ManualmenteCorrigido { get; set; } = false;

    /// <summary>
    /// Se deve ser descartada (erro de leitura, duplicada, etc.)
    /// </summary>
    public bool Descartada { get; set; } = false;

    [MaxLength(200)]
    public string? MotivoDescarte { get; set; }

    /// <summary>
    /// Hash único para detectar duplicatas
    /// </summary>
    [MaxLength(64)]
    public string? HashLeitura { get; set; }

    /// <summary>
    /// Dados brutos recebidos do coletor
    /// </summary>
    [MaxLength(500)]
    public string? DadosBrutos { get; set; }

    public DateTime DataRecebimento { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    [ForeignKey("IdEtapa")]
    public virtual Etapa Etapa { get; set; }

    [ForeignKey("IdInscricao")]
    public virtual Inscricao? Inscricao { get; set; }

    [ForeignKey("IdDispositivo")]
    public virtual DispositivoColetor? Dispositivo { get; set; }
}