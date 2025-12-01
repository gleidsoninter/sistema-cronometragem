using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class CategoriaCreateDto
{
    [Required(ErrorMessage = "Evento é obrigatório")]
    public int IdEvento { get; set; }

    [Required(ErrorMessage = "Modalidade é obrigatória")]
    public int IdModalidade { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; }

    [StringLength(500)]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Valor da inscrição é obrigatório")]
    [Range(0, 99999.99, ErrorMessage = "Valor deve estar entre 0 e 99999.99")]
    public decimal ValorInscricao { get; set; }

    [Range(0, 100, ErrorMessage = "Desconto deve estar entre 0 e 100%")]
    public decimal DescontoSegundaCategoria { get; set; } = 0;

    [Range(0, 100)]
    public int? IdadeMinima { get; set; }

    [Range(0, 100)]
    public int? IdadeMaxima { get; set; }

    [Range(50, 2000)]
    public int? CilindradaMinima { get; set; }

    [Range(50, 2000)]
    public int? CilindradaMaxima { get; set; }

    [RegularExpression("^(M|F|AMBOS)$", ErrorMessage = "Sexo deve ser M, F ou AMBOS")]
    public string Sexo { get; set; } = "AMBOS";

    public bool VagasLimitadas { get; set; } = false;

    [Range(1, 1000)]
    public int? NumeroVagas { get; set; }

    [Range(0, 100)]
    public int Ordem { get; set; } = 0;
}
