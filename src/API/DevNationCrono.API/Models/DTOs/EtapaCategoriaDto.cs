namespace DevNationCrono.API.Models.DTOs
{
    public class EtapaCategoriaDto
    {
        public int Id { get; set; }
        public int IdEtapa { get; set; }
        public int IdCategoria { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string SiglaCategoria { get; set; } = string.Empty;
        public string? CorCategoria { get; set; }
        public int OrdemLargada { get; set; }
        public int? TempoMinimoMinutos { get; set; }
        public int? NumeroVoltasEspecifico { get; set; }
    }

    public class EtapaCategoriaCreateDto
    {
        public int IdCategoria { get; set; }
        public int OrdemLargada { get; set; }
        public int? TempoMinimoMinutos { get; set; }
        public int? NumeroVoltasEspecifico { get; set; }
    }

    public class VincularCategoriasEtapaDto
    {
        /// <summary>
        /// Lista de categorias a vincular à etapa
        /// </summary>
        public List<EtapaCategoriaCreateDto> Categorias { get; set; } = new();
    }
}
