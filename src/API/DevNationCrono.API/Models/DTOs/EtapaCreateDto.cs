using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class EtapaCreateDto
{
    [Required(ErrorMessage = "Evento é obrigatório")]
    public int IdEvento { get; set; }

    [Required(ErrorMessage = "Número da etapa é obrigatório")]
    [Range(1, 100)]
    public int NumeroEtapa { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 3)]
    public string Nome { get; set; }

    [Required(ErrorMessage = "Data e hora são obrigatórias")]
    public DateTime DataHora { get; set; }

    // Configurações ENDURO
    [Range(1, 20)]
    public int NumeroEspeciais { get; set; } = 1;

    [Range(1, 10)]
    public int NumeroVoltas { get; set; } = 1;

    public bool PrimeiraVoltaValida { get; set; } = true;

    [Range(0, 7200, ErrorMessage = "Penalidade deve estar entre 0 e 7200 segundos (2 horas)")]
    public int PenalidadePorFaltaSegundos { get; set; } = 1200; // 20 minutos

    // Configurações CIRCUITO
    [Range(5, 480)]
    public int? DuracaoCorridaMinutos { get; set; }

    [Range(1, 5)]
    public int VoltasAposTempoMinimo { get; set; } = 2;

    [StringLength(1000)]
    public string? Observacoes { get; set; }
}
