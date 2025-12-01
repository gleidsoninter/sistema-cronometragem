namespace DevNationCrono.API.Models.DTOs;

public class InscricaoResumoDto
{
    public int Id { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string NomeCategoria { get; set; }
    public decimal ValorFinal { get; set; }
    public string StatusPagamento { get; set; }
    public DateTime DataInscricao { get; set; }
}
