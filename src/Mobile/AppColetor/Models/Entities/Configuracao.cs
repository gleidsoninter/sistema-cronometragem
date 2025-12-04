using SQLite;

namespace AppColetor.Models.Entities
{
    /// <summary>
    /// Configurações do app (chave-valor)
    /// </summary>
    [Table("configuracoes")]
    public class Configuracao
    {
        [PrimaryKey]
        public string Chave { get; set; } = "";

        public string Valor { get; set; } = "";

        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}