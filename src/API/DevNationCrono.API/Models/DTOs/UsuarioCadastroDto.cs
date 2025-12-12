using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class UsuarioCadastroDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role é obrigatória")]
    [RegularExpression("^(Admin|Organizador)$", ErrorMessage = "Role deve ser Admin ou Organizador")]
    public string Role { get; set; } = "Organizador";

    [MaxLength(20)]
    public string? Telefone { get; set; }
}

public class UsuarioAtualizacaoDto
{
    [MaxLength(100)]
    public string? Nome { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    public string? NovaSenha { get; set; }
}

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimoAcesso { get; set; }
}

public class LoginAdminRequestDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
}