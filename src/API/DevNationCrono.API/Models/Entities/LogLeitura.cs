using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("LogLeituras")]
public class LogLeitura
{
    [Key]
    public long Id { get; set; } // Mapeado para BIGINT (Essencial para logs)

    [Required]
    public int IdDispositivo { get; set; }

    [Required]
    public int NumeroMoto { get; set; }

    /// <summary>
    /// Timestamp original capturado pelo hardware (precisão de ms)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 'E' = Entrada, 'S' = Saída, 'P' = Passagem
    /// </summary>
    [Required]
    [MaxLength(1)]
    [Column(TypeName = "char(1)")]
    public string Tipo { get; set; }

    public int? IdEspecial { get; set; }

    public int Volta { get; set; }

    /// <summary>
    /// Payload completo recebido (Dump do objeto original para auditoria)
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")] // Mapeia para TEXT (string longa) no MySQL
    public string DadosJson { get; set; }

    /// <summary>
    /// Valores: 'PENDENTE', 'SINCRONIZADO', 'ERRO', 'DUPLICADO'
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";

    public int TentativasEnvio { get; set; } = 0;

    public DateTime? UltimaTentativa { get; set; }

    public string? MensagemErro { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataSincronizacao { get; set; }

    // --- Propriedades de Navegação ---

    [ForeignKey("IdDispositivo")]
    public virtual DispositivoColetor Dispositivo { get; set; }
}