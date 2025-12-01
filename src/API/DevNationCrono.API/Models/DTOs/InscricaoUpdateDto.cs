using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class InscricaoUpdateDto
{
    public int? NumeroMoto { get; set; }

    [StringLength(20)]
    public string? StatusPagamento { get; set; }

    [StringLength(20)]
    public string? MetodoPagamento { get; set; }

    [StringLength(100)]
    public string? TransacaoId { get; set; }

    public DateTime? DataPagamento { get; set; }

    [StringLength(500)]
    public string? Observacoes { get; set; }

    public bool? Ativo { get; set; }
}
