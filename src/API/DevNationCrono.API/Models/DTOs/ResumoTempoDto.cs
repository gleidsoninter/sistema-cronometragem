namespace DevNationCrono.API.Models.DTOs;

public class ResumoTempoDto
{
    public int NumeroMoto { get; set; }
    public int? IdInscricao { get; set; }
    public decimal TotalTempoSegundos { get; set; }
    public int TotalEspeciais { get; set; }
    public decimal? MelhorTempoSegundos { get; set; }
}
