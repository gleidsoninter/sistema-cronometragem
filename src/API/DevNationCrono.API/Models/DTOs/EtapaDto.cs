namespace DevNationCrono.API.Models.DTOs;

public class EtapaDto
{
    public int Id { get; set; }
    public int IdEvento { get; set; }
    public string NomeEvento { get; set; }
    public string TipoCronometragem { get; set; }
    public int NumeroEtapa { get; set; }
    public string Nome { get; set; }
    public DateTime DataHora { get; set; }

    // Configurações ENDURO
    public int NumeroEspeciais { get; set; }
    public int NumeroVoltas { get; set; }
    public bool PrimeiraVoltaValida { get; set; }
    public int PenalidadePorFaltaSegundos { get; set; }
    public string PenalidadeFormatada { get; set; } // "20:00"

    // Configurações CIRCUITO
    public int? DuracaoCorridaMinutos { get; set; }
    public int VoltasAposTempoMinimo { get; set; }

    public string Status { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCriacao { get; set; }

    // Calculados
    public int TotalLeituras { get; set; }
    public int TotalColetores { get; set; }
}
