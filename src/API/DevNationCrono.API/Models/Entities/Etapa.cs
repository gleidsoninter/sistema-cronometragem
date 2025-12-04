using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("etapas")]
public class Etapa
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IdEvento { get; set; }

    /// <summary>
    /// Número sequencial da etapa dentro do evento (1ª etapa, 2ª etapa, etc.)
    /// </summary>
    public int NumeroEtapa { get; set; } = 1;

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    [Required]
    public DateTime DataHora { get; set; }

    [MaxLength(200)]
    public string? Local { get; set; }

    // ============================================
    // CAMPOS PARA CIRCUITO FECHADO
    // (Motocross, Cross Country, Motovelocidade, SuperMoto)
    // ============================================

    /// <summary>
    /// Tempo total da prova em minutos (ex: 15, 20, 30 minutos)
    /// </summary>
    public int? TempoProvaMinutos { get; set; }

    /// <summary>
    /// Número de voltas previstas (alternativa ao tempo)
    /// </summary>
    public int? NumeroVoltas { get; set; }

    /// <summary>
    /// Número de voltas que o piloto pode completar após o tempo mínimo/bandeira
    /// Normalmente 1 ou 2 voltas adicionais após a bandeira
    /// </summary>
    public int VoltasAposTempoMinimo { get; set; } = 1;

    /// <summary>
    /// Hora exata da largada (quando a prova iniciou)
    /// </summary>
    public DateTime? HoraLargada { get; set; }

    /// <summary>
    /// Hora da bandeira (quando o tempo acabou ou completou voltas)
    /// </summary>
    public DateTime? HoraBandeira { get; set; }

    // ============================================
    // CAMPOS PARA ENDURO
    // ============================================

    /// <summary>
    /// Número de voltas no percurso (para Enduro)
    /// </summary>
    public int? NumeroVoltasEnduro { get; set; }

    /// <summary>
    /// Número de especiais por volta (para Enduro)
    /// </summary>
    public int? NumeroEspeciais { get; set; }

    /// <summary>
    /// Se a primeira volta é de reconhecimento e não conta tempo
    /// TRUE = primeira volta NÃO VALE (reconhecimento)
    /// FALSE = primeira volta VALE
    /// </summary>
    public bool VoltaReconhecimento { get; set; } = false;

    /// <summary>
    /// Inverso de VoltaReconhecimento - se a primeira volta é válida para pontuação
    /// TRUE = primeira volta VALE
    /// FALSE = primeira volta NÃO VALE (reconhecimento)
    /// </summary>
    [NotMapped]
    public bool PrimeiraVoltaValida
    {
        get => !VoltaReconhecimento;
        set => VoltaReconhecimento = !value;
    }

    /// <summary>
    /// Penalidade em segundos para quem não completar especial (Enduro)
    /// </summary>
    public int? PenalidadeSegundos { get; set; }

    /// <summary>
    /// Penalidade em segundos por falta/não comparecimento em especial
    /// Alias para PenalidadeSegundos para manter compatibilidade
    /// </summary>
    [NotMapped]
    public int? PenalidadePorFaltaSegundos
    {
        get => PenalidadeSegundos;
        set => PenalidadeSegundos = value;
    }

    // ============================================
    // CAMPOS DE CONTROLE
    // ============================================

    /// <summary>
    /// Status da etapa: NAO_INICIADA, EM_ANDAMENTO, BANDEIRA, FINALIZADA, CANCELADA
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "NAO_INICIADA";

    /// <summary>
    /// Observações gerais da etapa
    /// </summary>
    public string? Observacoes { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // ============================================
    // NAVEGAÇÃO
    // ============================================

    [ForeignKey("IdEvento")]
    public virtual Evento Evento { get; set; } = null!;

    public virtual ICollection<Inscricao> Inscricoes { get; set; } = new List<Inscricao>();

    public virtual ICollection<Tempo> Tempos { get; set; } = new List<Tempo>();

    /// <summary>
    /// Dispositivos coletores associados a esta etapa
    /// </summary>
    public virtual ICollection<DispositivoColetor> DispositivosColetores { get; set; } = new List<DispositivoColetor>();
}

