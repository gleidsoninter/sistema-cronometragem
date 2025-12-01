using DevNationCrono.API.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class PilotoCadastroDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 150 caracteres")]
    public string Nome { get; set; }

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(200)]
    public string Email { get; set; }

    [Required(ErrorMessage = "CPF é obrigatório")]
    [CpfValidation]
    public string Cpf { get; set; }

    [Required(ErrorMessage = "Telefone é obrigatório")]
    [Phone(ErrorMessage = "Telefone inválido")]
    [StringLength(15)]
    public string Telefone { get; set; }

    [Required(ErrorMessage = "Data de nascimento é obrigatória")]
    [DataType(DataType.Date)]
    public DateTime DataNascimento { get; set; }

    [Required(ErrorMessage = "Cidade é obrigatória")]
    [StringLength(100)]
    public string Cidade { get; set; }

    [Required(ErrorMessage = "UF é obrigatório")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "UF deve ter 2 caracteres")]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "UF deve ser em maiúsculas (ex: MG)")]
    public string Uf { get; set; }

    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
    [DataType(DataType.Password)]
    public string Senha { get; set; }

    // Campos opcionais
    [StringLength(60)]
    public string? Apelido { get; set; }

    [StringLength(100)]
    public string? ContatoEmergencia { get; set; }

    [Phone]
    [StringLength(15)]
    public string? TelefoneEmergencia { get; set; }

    [StringLength(50)]
    [RegularExpression(@"^@[\w\.]+$", ErrorMessage = "Instagram deve começar com @")]
    public string? Instagram { get; set; }

    [StringLength(200)]
    public string? Patrocinador { get; set; }
}
