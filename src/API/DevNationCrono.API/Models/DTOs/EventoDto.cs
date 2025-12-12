namespace DevNationCrono.API.Models.DTOs;

public class EventoDto
{
    public int Id { get; set; }
    public int? IdCampeonato { get; set; }
    public string? NomeCampeonato { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public string Local { get; set; }
    public string Cidade { get; set; }
    public string Uf { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public int IdModalidade { get; set; }
    public string NomeModalidade { get; set; }
    public string TipoCronometragem { get; set; }
    public string Status { get; set; }
    public bool InscricoesAbertas { get; set; }
    public DateTime? DataAberturaInscricoes { get; set; }
    public DateTime? DataFechamentoInscricoes { get; set; }
    public string? Regulamento { get; set; }
    public string? ImagemBanner { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }

    // Calculados
    public int TotalEtapas { get; set; }
    public int TotalCategorias { get; set; }
    public int TotalInscritos { get; set; }
}
