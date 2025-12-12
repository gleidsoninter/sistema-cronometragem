using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class EtapaUpdateDto
{
    /// <summary>
    /// ID do campeonato. Use null para desvincular do campeonato.
    /// </summary>
    public int? IdCampeonato { get; set; }

    [StringLength(100, MinimumLength = 3)]
    public string? Nome { get; set; }

    public DateTime? DataHora { get; set; }

    [Range(1, 20)]
    public int? NumeroEspeciais { get; set; }

    [Range(1, 10)]
    public int? NumeroVoltas { get; set; }

    public bool? PrimeiraVoltaValida { get; set; }

    [Range(0, 7200)]
    public int? PenalidadePorFaltaSegundos { get; set; }

    [Range(5, 480)]
    public int? DuracaoCorridaMinutos { get; set; }

    [Range(1, 5)]
    public int? VoltasAposTempoMinimo { get; set; }

    public string? Status { get; set; }

    [StringLength(1000)]
    public string? Observacoes { get; set; }
}
