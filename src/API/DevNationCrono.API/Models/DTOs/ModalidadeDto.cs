namespace DevNationCrono.API.Models.DTOs;

public class ModalidadeDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public string TipoCronometragem { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public int TotalEventos { get; set; } // Calculado
}
