using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("Pilotos")]
public class Piloto
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; }

    [Required]
    [MaxLength(200)]
    public string Email { get; set; }

    [MaxLength(60)]
    public string? Apelido { get; set; }

    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string Cpf { get; set; }

    [Required]
    [MaxLength(15)]
    public string Telefone { get; set; }

    [Required]
    public DateTime DataNascimento { get; set; }

    [MaxLength(100)]
    public string? ContatoEmergencia { get; set; }

    [MaxLength(15)]
    public string? TelefoneEmergencia { get; set; }

    [MaxLength(50)]
    public string? Instagram { get; set; }

    [MaxLength(200)]
    public string? Patrocinador { get; set; }

    [Required]
    [MaxLength(100)]
    public string Cidade { get; set; }

    [Required]
    [StringLength(2)]
    public string Uf { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordSalt { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // Navegação
    public virtual ICollection<Inscricao> Inscricoes { get; set; }
}

