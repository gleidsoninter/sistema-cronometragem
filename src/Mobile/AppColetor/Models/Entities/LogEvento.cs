using SQLite;

namespace AppColetor.Models.Entities
{
    [Table("log_eventos")]
    public class LogEvento
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Tipo: INFO, WARNING, ERROR, DEBUG
        /// </summary>
        [Indexed]
        public string Tipo { get; set; } = "INFO";

        /// <summary>
        /// Categoria: SERIAL, API, SYNC, APP
        /// </summary>
        [Indexed]
        public string Categoria { get; set; } = "APP";

        /// <summary>
        /// Mensagem do evento
        /// </summary>
        public string Mensagem { get; set; } = "";

        /// <summary>
        /// Dados adicionais em JSON
        /// </summary>
        public string? Dados { get; set; }

        /// <summary>
        /// Data/hora do evento
        /// </summary>
        [Indexed]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
    }
}