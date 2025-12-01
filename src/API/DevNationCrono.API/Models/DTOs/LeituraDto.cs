namespace DevNationCrono.API.Models.DTOs;

public class LeituraDto
{
    public int NumeroMoto { get; set; }
    public DateTime Timestamp { get; set; }
    public string Tipo { get; set; }
    public int? IdEspecial { get; set; }
    public int Volta { get; set; }
}
