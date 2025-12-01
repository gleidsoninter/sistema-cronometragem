using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class ModalidadeUpdateDto
{
    [StringLength(50, MinimumLength = 3)]
    public string? Nome { get; set; }

    [StringLength(500)]
    public string? Descricao { get; set; }

    public bool? Ativo { get; set; }
}
