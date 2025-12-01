using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class ModalidadeCreateDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 50 caracteres")]
    public string Nome { get; set; }

    [StringLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Tipo de cronometragem é obrigatório")]
    [RegularExpression("^(ENDURO|CIRCUITO)$", ErrorMessage = "Tipo deve ser ENDURO ou CIRCUITO")]
    public string TipoCronometragem { get; set; }
}
