using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("Inscricoes")]
public class Inscricao
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdPiloto { get; set; }

    [Required]
    public int IdEvento { get; set; }

    [Required]
    public int IdCategoria { get; set; }

    [Required]
    public int IdEtapa { get; set; }

    [Required]
    public int NumeroMoto { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorOriginal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PercentualDesconto { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorFinal { get; set; }

    [Required]
    [MaxLength(20)]
    public string StatusPagamento { get; set; } = "PENDENTE";
    // PENDENTE, AGUARDANDO_PAGAMENTO, PAGO, CANCELADO, REEMBOLSADO

    [MaxLength(20)]
    public string? MetodoPagamento { get; set; }
    // PIX, CARTAO, DINHEIRO, CORTESIA

    [MaxLength(100)]
    public string? TransacaoId { get; set; }

    [MaxLength(500)]
    public string? QrCodePix { get; set; }

    [MaxLength(100)]
    public string? CodigoPix { get; set; }

    public DateTime? DataPagamento { get; set; }

    public DateTime DataInscricao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    public bool Ativo { get; set; } = true;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    // Navegação
    [ForeignKey("IdPiloto")]
    public virtual Piloto Piloto { get; set; }

    [ForeignKey("IdEvento")]
    public virtual Evento Evento { get; set; }

    [ForeignKey("IdCategoria")]
    public virtual Categoria Categoria { get; set; }

    [ForeignKey("IdEtapa")]
    public virtual Etapa Etapa { get; set; }

    public virtual ICollection<Tempo> Tempos { get; set; }
}