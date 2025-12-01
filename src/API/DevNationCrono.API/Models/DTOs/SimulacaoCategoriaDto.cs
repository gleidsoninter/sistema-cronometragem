namespace DevNationCrono.API.Models.DTOs;

public class SimulacaoCategoriaDto
{
    public int IdCategoria { get; set; }
    public string NomeCategoria { get; set; }
    public decimal ValorOriginal { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorFinal { get; set; }
    public int Ordem { get; set; }
    public bool JaInscrito { get; set; }
}
