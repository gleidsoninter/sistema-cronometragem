using SQLite;

namespace AppColetor.Models.Entities
{
    [Table("dispositivo_coletor")]
    public class DispositivoColetor
    {
        [PrimaryKey]
        public string DeviceId { get; set; } = "";

        /// <summary>
        /// Nome amigável do coletor (ex: "Especial 1 - Entrada")
        /// </summary>
        public string Nome { get; set; } = "";

        /// <summary>
        /// Tipo: ENTRADA, SAIDA, PASSAGEM, LARGADA, CHEGADA, CONCENTRACAO
        /// </summary>
        public string Tipo { get; set; } = "PASSAGEM";

        /// <summary>
        /// ID do especial associado (se aplicável)
        /// </summary>
        public int? IdEspecial { get; set; }

        /// <summary>
        /// Nome do especial (para exibição)
        /// </summary>
        public string? NomeEspecial { get; set; }

        /// <summary>
        /// ID da etapa atual
        /// </summary>
        public int IdEtapa { get; set; }

        /// <summary>
        /// Identificador único do hardware Android
        /// </summary>
        public string? AndroidId { get; set; }

        /// <summary>
        /// Modelo do dispositivo Android
        /// </summary>
        public string? ModeloDispositivo { get; set; }

        /// <summary>
        /// Fabricante do dispositivo
        /// </summary>
        public string? Fabricante { get; set; }

        /// <summary>
        /// Versão do app instalada
        /// </summary>
        public string? VersaoApp { get; set; }

        /// <summary>
        /// Senha hash para autenticação
        /// </summary>
        public string? SenhaHash { get; set; }

        /// <summary>
        /// Status: ATIVO, INATIVO, MANUTENCAO
        /// </summary>
        public string Status { get; set; } = "ATIVO";

        /// <summary>
        /// Data de registro do dispositivo
        /// </summary>
        public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Último heartbeat recebido
        /// </summary>
        public DateTime? UltimoHeartbeat { get; set; }

        /// <summary>
        /// Última sincronização
        /// </summary>
        public DateTime? UltimaSync { get; set; }

        /// <summary>
        /// Nível de bateria (0-100)
        /// </summary>
        public int? NivelBateria { get; set; }

        /// <summary>
        /// Leituras pendentes de sync
        /// </summary>
        public int LeiturasPendentes { get; set; }

        /// <summary>
        /// Total de leituras na sessão
        /// </summary>
        public int TotalLeiturasSessao { get; set; }

        /// <summary>
        /// Offset de tempo em milissegundos (para sincronização NTP)
        /// </summary>
        public long OffsetTempoMs { get; set; }

        /// <summary>
        /// Observações
        /// </summary>
        public string? Observacoes { get; set; }
    }
}