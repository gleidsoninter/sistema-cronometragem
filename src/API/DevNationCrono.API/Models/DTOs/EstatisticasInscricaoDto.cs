namespace DevNationCrono.API.Models.DTOs;

public class EstatisticasInscricaoDto
{
    public int TotalInscritos { get; set; }
    public int TotalPagos { get; set; }
    public int TotalPendentes { get; set; }
    public int TotalCancelados { get; set; }
    public decimal ValorTotalArrecadado { get; set; }
    public decimal ValorTotalPendente { get; set; }
    public Dictionary<string, int> InscritosPorCategoria { get; set; }
}
