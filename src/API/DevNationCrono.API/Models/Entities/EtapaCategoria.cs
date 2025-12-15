using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevNationCrono.API.Models.Entities
{
    [Table("etapa_categorias")]
    public class EtapaCategoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdEtapa { get; set; }

        [Required]
        public int IdCategoria { get; set; }

        /// <summary>
        /// Ordem de largada/exibição desta categoria na etapa
        /// </summary>
        public int OrdemLargada { get; set; }

        /// <summary>
        /// Tempo mínimo específico para esta categoria (sobrescreve configuração da etapa)
        /// </summary>
        public int? TempoMinimoMinutos { get; set; }

        /// <summary>
        /// Número de voltas específico para esta categoria (sobrescreve configuração da etapa)
        /// </summary>
        public int? NumeroVoltasEspecifico { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("IdEtapa")]
        public virtual Etapa? Etapa { get; set; }

        [ForeignKey("IdCategoria")]
        public virtual Categoria? Categoria { get; set; }
    }

}
