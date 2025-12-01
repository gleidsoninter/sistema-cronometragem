using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("Etapas")]
public class Etapa
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdEvento { get; set; }

    [Required]
    public int NumeroEtapa { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    [Required]
    public DateTime DataHora { get; set; }

    public int NumeroEspeciais { get; set; } = 1;
    public int NumeroVoltas { get; set; } = 1;
    public bool PrimeiraVoltaValida { get; set; } = true;
    public int PenalidadePorFaltaSegundos { get; set; } = 1200;
    public int? DuracaoCorridaMinutos { get; set; }
    public int VoltasAposTempoMinimo { get; set; } = 2;

    [MaxLength(50)]
    public string Status { get; set; } = "AGENDADA";

    public string? Observacoes { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("IdEvento")]
    public virtual Evento Evento { get; set; }

    public virtual ICollection<Inscricao> Inscricoes { get; set; }
    public virtual ICollection<Tempo> Tempos { get; set; }
    public virtual ICollection<DispositivoColetor> DispositivosColetores { get; set; }
}

