using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class ConfirmarPagamentoDto
{
    [Required]
    [StringLength(20)]
    public string MetodoPagamento { get; set; }

    [StringLength(100)]
    public string? TransacaoId { get; set; }

    [StringLength(500)]
    public string? Observacoes { get; set; }
}
