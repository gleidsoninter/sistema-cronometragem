using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("pagamentos")]
public class Pagamento
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdInscricao { get; set; }

    [Required]
    [MaxLength(100)]
    public string IdExterno { get; set; }

    [Required]
    [MaxLength(50)]
    public string Gateway { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; }

    [MaxLength(500)]
    public string? QrCode { get; set; }

    public string? QrCodeBase64 { get; set; }

    [MaxLength(500)]
    public string? CopiaCola { get; set; }

    [MaxLength(100)]
    public string? TransacaoId { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataExpiracao { get; set; }

    public DateTime? DataPagamento { get; set; }

    public DateTime? DataAtualizacao { get; set; }

    public string? PayloadOriginal { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    // Navegação
    [ForeignKey("IdInscricao")]
    public virtual Inscricao Inscricao { get; set; }
}