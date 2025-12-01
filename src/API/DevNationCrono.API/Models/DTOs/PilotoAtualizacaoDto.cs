using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class PilotoAtualizacaoDto
{
    [StringLength(150, MinimumLength = 3)]
    public string? Nome { get; set; }

    [Phone]
    [StringLength(15)]
    public string? Telefone { get; set; }

    [StringLength(50)]
    [RegularExpression(@"^@[\w\.]+$")]
    public string? Instagram { get; set; }

    [StringLength(200)]
    public string? Patrocinador { get; set; }

    [StringLength(100)]
    public string? ContatoEmergencia { get; set; }

    [Phone]
    [StringLength(15)]
    public string? TelefoneEmergencia { get; set; }
}
