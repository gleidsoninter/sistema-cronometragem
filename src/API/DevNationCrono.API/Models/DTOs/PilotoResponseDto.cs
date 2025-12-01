namespace DevNationCrono.API.Models.DTOs;

public class PilotoResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string? Apelido { get; set; }
    public string Cpf { get; set; }
    public string Telefone { get; set; }
    public DateTime DataNascimento { get; set; }
    public int Idade { get; set; } // Calculado
    public string? ContatoEmergencia { get; set; }
    public string? TelefoneEmergencia { get; set; }
    public string? Instagram { get; set; }
    public string? Patrocinador { get; set; }
    public string Cidade { get; set; }
    public string Uf { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
}
