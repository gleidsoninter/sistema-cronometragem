namespace DevNationCrono.API.Models.DTOs;

public class CategoriaDto
{
    public int Id { get; set; }
    public int IdEvento { get; set; }
    public string NomeEvento { get; set; }
    public int IdModalidade { get; set; }
    public string NomeModalidade { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal ValorInscricao { get; set; }
    public decimal DescontoSegundaCategoria { get; set; }
    public int? IdadeMinima { get; set; }
    public int? IdadeMaxima { get; set; }
    public int? CilindradaMinima { get; set; }
    public int? CilindradaMaxima { get; set; }
    public string Sexo { get; set; }
    public bool VagasLimitadas { get; set; }
    public int? NumeroVagas { get; set; }
    public int VagasDisponiveis { get; set; } // Calculado
    public bool Ativo { get; set; }
    public int Ordem { get; set; }
    public int TotalInscritos { get; set; }
}
