using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities
{
    [Table("resultados")]
    public class Resultado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdEtapa { get; set; }

        [Required]
        public int IdCategoria { get; set; }

        [Required]
        public int IdInscricao { get; set; }

        // Resultado
        public int? Posicao { get; set; }
        public int? PosicaoGeral { get; set; }

        // Dados da corrida
        public int TotalVoltas { get; set; }

        /// <summary>
        /// Tempo total em milissegundos
        /// </summary>
        public long? TempoTotal { get; set; }

        /// <summary>
        /// Melhor volta em milissegundos
        /// </summary>
        public long? MelhorVolta { get; set; }

        public int? VoltaMelhorTempo { get; set; }

        // Diferenças
        public long? DiferencaLider { get; set; }
        public long? DiferencaAnterior { get; set; }
        public int? VoltasAtras { get; set; }

        // Status
        [StringLength(20)]
        public string Status { get; set; } = "CLASSIFICADO";

        [StringLength(200)]
        public string? MotivoStatus { get; set; }

        // Pontuação (campeonato)
        [Column(TypeName = "decimal(10,2)")]
        public decimal PontosObtidos { get; set; }

        public bool PontuacaoAplicada { get; set; }

        // Penalidades
        public int PenalidadeSegundos { get; set; }

        [StringLength(200)]
        public string? MotivoPenalidade { get; set; }

        // Controle de processamento
        public DateTime? ProcessadoEm { get; set; }
        public int? ProcessadoPor { get; set; }

        // Homologação
        public bool Homologado { get; set; }
        public DateTime? HomologadoEm { get; set; }
        public int? HomologadoPor { get; set; }

        // Controle
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }

        // Navigation
        [ForeignKey("IdEtapa")]
        public virtual Etapa? Etapa { get; set; }

        [ForeignKey("IdCategoria")]
        public virtual Categoria? Categoria { get; set; }

        [ForeignKey("IdInscricao")]
        public virtual Inscricao? Inscricao { get; set; }
    }
}
