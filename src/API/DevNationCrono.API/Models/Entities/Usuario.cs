using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

/// <summary>
/// Usuário administrativo (Admin, Organizador)
/// Separado de Piloto pois são perfis diferentes
/// </summary>
[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Organizador";  // Admin, Organizador

    [Required]
    [MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PasswordSalt { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoAcesso { get; set; }
}