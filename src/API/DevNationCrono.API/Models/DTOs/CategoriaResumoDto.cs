namespace DevNationCrono.API.Models.DTOs;

public class CategoriaResumoDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public decimal ValorInscricao { get; set; }
    public int TotalInscritos { get; set; }
    public int? VagasDisponiveis { get; set; }
}
