namespace DevNationCrono.API.Models.DTOs;

public class EventoResumoDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Local { get; set; }
    public string Cidade { get; set; }
    public string Uf { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string NomeModalidade { get; set; }
    public string TipoCronometragem { get; set; }
    public string Status { get; set; }
    public bool InscricoesAbertas { get; set; }
    public int TotalInscritos { get; set; }
}
