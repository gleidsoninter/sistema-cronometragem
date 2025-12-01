namespace DevNationCrono.API.Models.DTOs;

public class EtapaResumoDto
{
    public int Id { get; set; }
    public int NumeroEtapa { get; set; }
    public string Nome { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; }
}
