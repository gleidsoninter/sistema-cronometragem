namespace DevNationCrono.API.Models.DTOs;

public class TempoCalculadoDto
{
    public long IdTempo { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public int Volta { get; set; }
    public int? IdEspecial { get; set; }
    public DateTime? Entrada { get; set; }
    public DateTime? Saida { get; set; }
    public decimal? TempoSegundos { get; set; }
    public string TempoFormatado { get; set; }
    public bool Penalizado { get; set; }
    public int PenalidadeSegundos { get; set; }
    public string MotivoPenalidade { get; set; }
}
