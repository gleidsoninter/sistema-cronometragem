using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class CorrecaoTempoDto
{
    public int? NumeroMoto { get; set; }

    public DateTime? Timestamp { get; set; }

    public string? Tipo { get; set; }

    public int? IdEspecial { get; set; }

    public int? Volta { get; set; }

    public bool? Descartar { get; set; }

    [MaxLength(200)]
    public string? Motivo { get; set; }
}
