using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class LeituraItemDto
{
    [Required]
    public int NumeroMoto { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    public string Tipo { get; set; }

    public int? IdEspecial { get; set; }

    public int Volta { get; set; } = 1;

    public string? IdLocal { get; set; }

    public string? DadosBrutos { get; set; }
}
