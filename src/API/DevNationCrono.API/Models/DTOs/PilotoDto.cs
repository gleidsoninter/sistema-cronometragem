namespace DevNationCrono.API.Models.DTOs;

public class PilotoDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string? Apelido { get; set; }
    public string Cpf { get; set; }
    public string Telefone { get; set; }
    public DateTime DataNascimento { get; set; }
    public string? Instagram { get; set; }
    public string Cidade { get; set; }
    public string Uf { get; set; }
    // NÃO EXPÕE PasswordHash/Salt!
}
