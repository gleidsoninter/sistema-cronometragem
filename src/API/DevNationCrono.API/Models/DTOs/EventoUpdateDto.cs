using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class EventoUpdateDto
{
    [StringLength(200, MinimumLength = 5)]
    public string? Nome { get; set; }

    [StringLength(2000)]
    public string? Descricao { get; set; }

    [StringLength(200)]
    public string? Local { get; set; }

    [StringLength(100)]
    public string? Cidade { get; set; }

    [StringLength(2, MinimumLength = 2)]
    public string? Uf { get; set; }

    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }

    public string? Status { get; set; }
    public bool? InscricoesAbertas { get; set; }
    public DateTime? DataAberturaInscricoes { get; set; }
    public DateTime? DataFechamentoInscricoes { get; set; }

    [StringLength(5000)]
    public string? Regulamento { get; set; }

    [StringLength(500)]
    public string? ImagemBanner { get; set; }

    public bool? Ativo { get; set; }
}
