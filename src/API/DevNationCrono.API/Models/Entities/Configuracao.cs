using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities;

[Table("configuracoes")]
public class Configuracao
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Chave única de identificação (ex: "SISTEMA_MANUTENCAO", "PIX_CHAVE_PADRAO")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Chave { get; set; }

    /// <summary>
    /// O valor é salvo como TEXT para suportar desde um simples "true" até um JSON complexo
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")]
    public string Valor { get; set; }

    /// <summary>
    /// Metadado para ajudar o Frontend ou Backend a fazer o cast correto:
    /// 'STRING', 'INT', 'BOOLEAN', 'JSON', 'DECIMAL'
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; }

    [Column(TypeName = "TEXT")]
    public string? Descricao { get; set; }

    /// <summary>
    /// Agrupamento para facilitar a busca na tela de admin:
    /// 'SISTEMA', 'PAGAMENTO', 'EMAIL', 'NOTIFICACAO', 'CRONOMETRAGEM'
    /// </summary>
    [MaxLength(20)]
    public string Categoria { get; set; } = "SISTEMA";

    /// <summary>
    /// Define se o admin pode alterar este valor pela tela ou se é fixo (Hard Config)
    /// </summary>
    public bool Editavel { get; set; } = true;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
}