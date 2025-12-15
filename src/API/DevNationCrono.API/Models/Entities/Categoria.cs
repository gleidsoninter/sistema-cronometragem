using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("categorias")]
public class Categoria
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdModalidade { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    public string? Descricao { get; set; }
    public string? Sigla { get; set; }
    public string? Cor { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")] // Garante a precisão correta no banco
    public decimal ValorInscricao { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DescontoSegundaCategoria { get; set; } = 0.00m;

    public int? IdadeMinima { get; set; }

    public int? IdadeMaxima { get; set; }

    // CC da moto
    public int? CilindradaMinima { get; set; }

    public int? CilindradaMaxima { get; set; }

    // Mapeado como string para simplificar, mas poderia ser um Enum no C#
    [MaxLength(10)]
    public string Sexo { get; set; } = "AMBOS";

    public bool VagasLimitadas { get; set; } = false;

    public int? NumeroVagas { get; set; }

    public bool Ativo { get; set; } = true;

    public int Ordem { get; set; } = 0;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;


    [ForeignKey("IdModalidade")]
    public virtual Modalidade Modalidade { get; set; }

    // Assumindo que uma Categoria tem várias Inscrições (igual a Etapa)
    public virtual ICollection<Inscricao> Inscricoes { get; set; }
}