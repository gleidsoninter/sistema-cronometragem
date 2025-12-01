using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("Eventos")]
public class Evento
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; }

    public string? Descricao { get; set; }

    [Required]
    [MaxLength(200)]
    public string Local { get; set; }

    [Required]
    [MaxLength(100)]
    public string Cidade { get; set; }

    [Required]
    [StringLength(2)]
    public string Uf { get; set; }

    [Required]
    public DateTime DataInicio { get; set; }

    [Required]
    public DateTime DataFim { get; set; }

    [Required]
    public int IdModalidade { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "AGENDADO";

    public bool InscricoesAbertas { get; set; } = false;

    public DateTime? DataAberturaInscricoes { get; set; }

    public DateTime? DataFechamentoInscricoes { get; set; }

    public string? Regulamento { get; set; }

    [MaxLength(500)]
    public string? ImagemBanner { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("IdModalidade")]
    public virtual Modalidade Modalidade { get; set; }

    public virtual ICollection<Etapa> Etapas { get; set; }
    public virtual ICollection<Categoria> Categorias { get; set; }
    public virtual ICollection<Inscricao> Inscricoes { get; set; }
}

