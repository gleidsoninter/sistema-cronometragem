using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class CategoriaUpdateDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? Nome { get; set; }

    [StringLength(500)]
    public string? Descricao { get; set; }

    [Range(0, 99999.99)]
    public decimal? ValorInscricao { get; set; }

    [Range(0, 100)]
    public decimal? DescontoSegundaCategoria { get; set; }

    public int? IdadeMinima { get; set; }
    public int? IdadeMaxima { get; set; }
    public int? CilindradaMinima { get; set; }
    public int? CilindradaMaxima { get; set; }

    public bool? VagasLimitadas { get; set; }
    public int? NumeroVagas { get; set; }
    public int? Ordem { get; set; }
    public bool? Ativo { get; set; }
}
