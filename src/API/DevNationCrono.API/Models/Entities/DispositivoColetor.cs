using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("DispositivosColetores")]
public class DispositivoColetor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdEvento { get; set; }

    [Required]
    public int IdEtapa { get; set; }

    /// <summary>
    /// Nome identificador do dispositivo (Ex: "Celular Largada 1")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    /// <summary>
    /// Valores: 'ENTRADA', 'SAIDA', 'PASSAGEM'
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; }

    /// <summary>
    /// Para Enduro: ID da especial que este dispositivo monitora.
    /// </summary>
    public int? IdEspecial { get; set; }

    /// <summary>
    /// ID único do hardware (Android ID ou UUID)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; }

    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(20)]
    public string? VersaoApp { get; set; }

    /// <summary>
    /// Token JWT para autenticação do dispositivo na API
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Token { get; set; }

    public DateTime? UltimaConexao { get; set; }

    public DateTime? UltimaSincronizacao { get; set; }

    public int LeiturasPendentes { get; set; } = 0;

    /// <summary>
    /// Valores: 'ONLINE', 'OFFLINE', 'INATIVO'
    /// </summary>
    [MaxLength(20)]
    public string StatusConexao { get; set; } = "OFFLINE";

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // --- Propriedades de Navegação ---

    [ForeignKey("IdEvento")]
    public virtual Evento Evento { get; set; }

    [ForeignKey("IdEtapa")]
    public virtual Etapa Etapa { get; set; }
}