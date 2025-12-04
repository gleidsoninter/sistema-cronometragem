using SQLite;

namespace AppColetor.Models.Entities
{
    /// <summary>
    /// Leitura armazenada localmente
    /// </summary>
    [Table("leituras")]
    public class Leitura
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        /// <summary>
        /// Número da moto/transponder
        /// </summary>
        [Indexed]
        public int NumeroMoto { get; set; }

        /// <summary>
        /// Timestamp da leitura (UTC)
        /// </summary>
        [Indexed]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Tipo de leitura: P (passagem), E (entrada), S (saída)
        /// </summary>
        public string Tipo { get; set; } = "P";

        /// <summary>
        /// ID da etapa
        /// </summary>
        [Indexed]
        public int IdEtapa { get; set; }

        /// <summary>
        /// Número da volta (para circuito)
        /// </summary>
        public int? Volta { get; set; }

        /// <summary>
        /// ID da especial (para enduro)
        /// </summary>
        public int? IdEspecial { get; set; }

        /// <summary>
        /// ID do dispositivo coletor
        /// </summary>
        public string DeviceId { get; set; } = "";

        /// <summary>
        /// Hash único para evitar duplicatas
        /// </summary>
        [Indexed]
        public string Hash { get; set; } = "";

        /// <summary>
        /// Dados brutos recebidos do coletor
        /// </summary>
        public string DadosBrutos { get; set; } = "";

        /// <summary>
        /// Indica se foi sincronizado com a API
        /// </summary>
        [Indexed]
        public bool Sincronizado { get; set; }

        /// <summary>
        /// Data/hora da sincronização
        /// </summary>
        public DateTime? DataSincronizacao { get; set; }

        /// <summary>
        /// Resposta da API (para debug)
        /// </summary>
        public string? RespostaApi { get; set; }

        /// <summary>
        /// Erro de sincronização (se houver)
        /// </summary>
        public string? ErroSync { get; set; }

        /// <summary>
        /// Tentativas de sincronização
        /// </summary>
        public int TentativasSync { get; set; }

        /// <summary>
        /// Data de criação do registro
        /// </summary>
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gera hash único para a leitura
        /// </summary>
        public void GerarHash()
        {
            var dados = $"{IdEtapa}_{NumeroMoto}_{Timestamp:yyyyMMddHHmmssfff}_{Tipo}_{IdEspecial}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dados));
            Hash = Convert.ToHexString(bytes)[..16]; // Primeiros 16 caracteres
        }
    }
}