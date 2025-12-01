using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class SimularInscricaoDto
{
    [Required]
    public int IdPiloto { get; set; }

    [Required]
    public int IdEvento { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> IdsCategorias { get; set; }
}
