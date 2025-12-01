using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("Modalidades")]
public class Modalidade
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nome { get; set; }

    public string? Descricao { get; set; }

    [Required]
    [Column(TypeName = "varchar(10)")]
    public string TipoCronometragem { get; set; } // "ENDURO" ou "CIRCUITO"

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegação
    public virtual ICollection<Evento> Eventos { get; set; }
    public virtual ICollection<Categoria> Categorias { get; set; }
}

