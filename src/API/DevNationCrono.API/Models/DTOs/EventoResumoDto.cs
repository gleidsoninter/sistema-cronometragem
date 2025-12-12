namespace DevNationCrono.API.Models.DTOs;

public class EventoResumoDto
{
    public int Id { get; set; }
    public int? IdCampeonato { get; set; }
    public string? NomeCampeonato { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Local { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int IdModalidade { get; set; }
    public string NomeModalidade { get; set; } = string.Empty;
    public string? TipoCronometragem { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool InscricoesAbertas { get; set; }
    public int TotalInscritos { get; set; }
}
