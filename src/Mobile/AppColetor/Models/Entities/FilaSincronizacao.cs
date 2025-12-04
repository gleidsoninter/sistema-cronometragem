using SQLite;

namespace AppColetor.Models.Entities
{
    [Table("fila_sincronizacao")]
    public class FilaSincronizacao
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// ID da leitura relacionada
        /// </summary>
        [Indexed]
        public int IdLeitura { get; set; }

        /// <summary>
        /// Status: PENDENTE, EM_PROCESSAMENTO, CONCLUIDO, ERRO
        /// </summary>
        [Indexed]
        public string Status { get; set; } = "PENDENTE";

        /// <summary>
        /// Prioridade (maior = mais urgente)
        /// </summary>
        [Indexed]
        public int Prioridade { get; set; } = 0;

        /// <summary>
        /// Número de tentativas de envio
        /// </summary>
        public int Tentativas { get; set; } = 0;

        /// <summary>
        /// Máximo de tentativas permitidas
        /// </summary>
        public int MaxTentativas { get; set; } = 5;

        /// <summary>
        /// Última mensagem de erro
        /// </summary>
        public string? UltimoErro { get; set; }

        /// <summary>
        /// Data/hora da próxima tentativa
        /// </summary>
        public DateTime? ProximaTentativa { get; set; }

        /// <summary>
        /// Data de criação
        /// </summary>
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data da última tentativa
        /// </summary>
        public DateTime? UltimaTentativa { get; set; }

        /// <summary>
        /// Data de conclusão
        /// </summary>
        public DateTime? DataConclusao { get; set; }
    }
}