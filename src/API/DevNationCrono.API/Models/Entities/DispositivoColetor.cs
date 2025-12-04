using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("dispositivoscoletores")]
public class DispositivoColetor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdEvento { get; set; }

    [Required]
    public int IdEtapa { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } // Ex: "Coletor Especial 1 - Entrada"

    [Required]
    [MaxLength(1)]
    public string Tipo { get; set; } // E, S, P

    /// <summary>
    /// Qual especial este coletor monitora (ENDURO)
    /// </summary>
    public int? IdEspecial { get; set; }

    /// <summary>
    /// Identificador único do dispositivo Android
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; }

    /// <summary>
    /// Token JWT específico deste dispositivo
    /// </summary>
    [MaxLength(500)]
    public string? Token { get; set; }

    public DateTime? UltimaConexao { get; set; }

    [MaxLength(20)]
    public string StatusConexao { get; set; } = "OFFLINE";
    // ONLINE, OFFLINE, SINCRONIZANDO

    /// <summary>
    /// Última leitura enviada por este dispositivo
    /// </summary>
    public DateTime? UltimaLeitura { get; set; }

    public int TotalLeituras { get; set; } = 0;

    //public string? SenhaHash { get; set; }
    [Column("ativo", TypeName = "tinyint(1)")]
    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("IdEvento")]
    public virtual Evento Evento { get; set; }

    [ForeignKey("IdEtapa")]
    public virtual Etapa Etapa { get; set; }

    //public virtual ICollection<Tempo> Tempos { get; set; }
}