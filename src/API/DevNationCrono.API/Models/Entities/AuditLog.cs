using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("AuditLog")]
public class AuditLog
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Nome da tabela que sofreu alteração (Ex: "Inscricoes", "Pilotos")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Tabela { get; set; }

    /// <summary>
    /// ID do registro alterado.
    /// OBS: Se você usar GUID em alguma tabela futura, precisará mudar isso para string/varchar.
    /// </summary>
    [Required]
    public int RegistroId { get; set; }

    /// <summary>
    /// Valores: 'INSERT', 'UPDATE', 'DELETE'
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Acao { get; set; }

    public int? UsuarioId { get; set; }

    /// <summary>
    /// Valores: 'PILOTO', 'ORGANIZADOR', 'ADMIN', 'SISTEMA', 'COLETOR'
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string UsuarioTipo { get; set; }

    /// <summary>
    /// Snapshot dos dados ANTES da alteração (JSON)
    /// </summary>
    [Column(TypeName = "json")]
    public string? DadosAntigos { get; set; }

    /// <summary>
    /// Snapshot dos dados DEPOIS da alteração (JSON)
    /// </summary>
    [Column(TypeName = "json")]
    public string? DadosNovos { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime DataHora { get; set; } = DateTime.UtcNow;
}