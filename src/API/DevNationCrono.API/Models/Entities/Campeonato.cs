using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

/// <summary>
/// Representa um campeonato que agrupa vários eventos
/// </summary>
[Table("campeonatos")]
public class Campeonato
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Sigla { get; set; }

    [Required]
    public int Ano { get; set; }

    public string? Descricao { get; set; }

    public string? Regulamento { get; set; }

    [StringLength(500)]
    public string? ImagemBanner { get; set; }

    [Required]
    public int IdModalidade { get; set; }

    /// <summary>
    /// Quantidade de melhores resultados que contam para o campeonato.
    /// NULL = todos os resultados contam.
    /// </summary>
    public int? QtdeEtapasValidas { get; set; }

    // =====================================
    // REGRAS DE PONTUAÇÃO - CIRCUITO
    // =====================================

    /// <summary>
    /// Percentual mínimo de voltas do líder para pontuar.
    /// Ex: 0.20 = 20%, 0.50 = 50%, NULL = qualquer quantidade pontua
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentualMinimoVoltasLider { get; set; }

    /// <summary>
    /// Se TRUE, piloto precisa tomar bandeirada para pontuar.
    /// </summary>
    public bool ExigeBandeirada { get; set; }

    // =====================================
    // REGRAS DE PONTUAÇÃO - ENDURO
    // =====================================

    /// <summary>
    /// Percentual mínimo da prova completada para pontuar.
    /// Ex: 0.50 = 50% dos tempos possíveis
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentualMinimoProvaEnduro { get; set; }

    // =====================================
    // REGRAS DE PONTUAÇÃO - GERAIS
    // =====================================

    /// <summary>
    /// Se TRUE, qualquer piloto que participou pontua (ignora regras mínimas).
    /// </summary>
    public bool TodosParticipantesPontuam { get; set; }

    /// <summary>
    /// Se TRUE, desclassificado (DSQ - corte caminho, ato anti-esportivo) NÃO pontua.
    /// </summary>
    public bool DesclassificadoNaoPontua { get; set; } = true;

    /// <summary>
    /// Se TRUE, abandono (DNF) NÃO pontua.
    /// </summary>
    public bool AbandonoNaoPontua { get; set; } = true;

    // =====================================
    // CONTROLE
    // =====================================

    [StringLength(20)]
    public string Status { get; set; } = "PLANEJADO";

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // =====================================
    // NAVEGAÇÃO
    // =====================================

    [ForeignKey("IdModalidade")]
    public virtual Modalidade? Modalidade { get; set; }

    public virtual ICollection<CampeonatoPontuacao> Pontuacoes { get; set; } = new List<CampeonatoPontuacao>();

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}

/// <summary>
/// Representa a pontuação por posição em um campeonato
/// </summary>
[Table("campeonato_pontuacoes")]
public class CampeonatoPontuacao
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdCampeonato { get; set; }

    /// <summary>
    /// Posição que pontua (1, 2, 3, ...)
    /// </summary>
    [Required]
    public int Posicao { get; set; }

    /// <summary>
    /// Pontos ganhos nesta posição
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Pontos { get; set; }

    // =====================================
    // NAVEGAÇÃO
    // =====================================

    [ForeignKey("IdCampeonato")]
    public virtual Campeonato? Campeonato { get; set; }
}
