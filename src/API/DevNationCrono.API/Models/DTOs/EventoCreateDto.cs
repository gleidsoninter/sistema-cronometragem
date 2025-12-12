using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class EventoCreateDto
{
    /// <summary>
    /// ID do campeonato (opcional). Se preenchido, vincula o evento ao campeonato.
    /// </summary>
    public int? IdCampeonato { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Nome deve ter entre 5 e 200 caracteres")]
    public string Nome { get; set; }

    [StringLength(2000)]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Local é obrigatório")]
    [StringLength(200)]
    public string Local { get; set; }

    [Required(ErrorMessage = "Cidade é obrigatória")]
    [StringLength(100)]
    public string Cidade { get; set; }

    [Required(ErrorMessage = "UF é obrigatório")]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "UF deve ter 2 letras maiúsculas")]
    public string Uf { get; set; }

    [Required(ErrorMessage = "Data de início é obrigatória")]
    public DateTime DataInicio { get; set; }

    [Required(ErrorMessage = "Data de fim é obrigatória")]
    public DateTime DataFim { get; set; }

    [Required(ErrorMessage = "Modalidade é obrigatória")]
    public int IdModalidade { get; set; }

    public DateTime? DataAberturaInscricoes { get; set; }
    public DateTime? DataFechamentoInscricoes { get; set; }

    [StringLength(5000)]
    public string? Regulamento { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "URL da imagem inválida")]
    public string? ImagemBanner { get; set; }
}
