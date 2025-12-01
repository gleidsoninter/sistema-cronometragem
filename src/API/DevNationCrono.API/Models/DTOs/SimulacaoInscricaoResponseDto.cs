namespace DevNationCrono.API.Models.DTOs;

public class SimulacaoInscricaoResponseDto
{
    public string TipoCronometragem { get; set; }
    public bool PermiteMultiplasCategorias { get; set; }
    public decimal PercentualDescontoConfigurado { get; set; }
    public List<SimulacaoCategoriaDto> Categorias { get; set; }
    public decimal ValorTotalOriginal { get; set; }
    public decimal ValorTotalDesconto { get; set; }
    public decimal ValorTotalFinal { get; set; }
    public List<string> Avisos { get; set; }
}
