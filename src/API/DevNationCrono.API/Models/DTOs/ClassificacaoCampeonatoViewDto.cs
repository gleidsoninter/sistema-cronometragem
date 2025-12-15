namespace DevNationCrono.API.Models.DTOs
{
    public class ClassificacaoCampeonatoViewDto
    {
        public int IdCampeonato { get; set; }
        public string NomeCampeonato { get; set; } = string.Empty;
        public int AnoCampeonato { get; set; }
        public int IdCategoria { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string SiglaCategoria { get; set; } = string.Empty;
        public int IdPiloto { get; set; }
        public string NomePiloto { get; set; } = string.Empty;
        public string? Apelido { get; set; }
        public int Numero { get; set; }

        public int EtapasParticipadas { get; set; }
        public decimal TotalPontos { get; set; }
        public int Vitorias { get; set; }
        public int Podios { get; set; }
        public int? MelhorPosicao { get; set; }
        public string? Posicoes { get; set; }
        public string? PontosPorEtapa { get; set; }
    }
}
