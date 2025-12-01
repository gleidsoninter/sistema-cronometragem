namespace DevNationCrono.API.Models.DTOs;

public class ResultadoPilotoEnduroDto
{
    public int Posicao { get; set; }
    public int PosicaoCategoria { get; set; }

    // Piloto
    public int IdInscricao { get; set; }
    public int IdPiloto { get; set; }
    public string NomePiloto { get; set; }
    public string Cidade { get; set; }
    public string Uf { get; set; }
    public int NumeroMoto { get; set; }

    // Categoria
    public int IdCategoria { get; set; }
    public string NomeCategoria { get; set; }

    // Tempos
    public decimal TempoTotalSegundos { get; set; }
    public string TempoTotalFormatado { get; set; }
    public decimal? DiferencaLiderSegundos { get; set; }
    public string DiferencaLiderFormatado { get; set; }
    public decimal? DiferencaAnteriorSegundos { get; set; }
    public string DiferencaAnteriorFormatado { get; set; }

    // Penalidades
    public int TotalPenalidades { get; set; }
    public decimal TotalPenalidadeSegundos { get; set; }
    public string TotalPenalidadeFormatado { get; set; }

    // Estatísticas
    public int EspeciaisCompletadas { get; set; }
    public int EspeciaisPenalizadas { get; set; }
    public int TotalEspeciais { get; set; }
    public decimal? MelhorTempoEspecialSegundos { get; set; }
    public string MelhorTempoEspecialFormatado { get; set; }

    // Status
    public string Status { get; set; } // CLASSIFICADO, DESCLASSIFICADO, NAO_LARGOU, ABANDONO
    public string MotivoStatus { get; set; }

    // Detalhamento por volta/especial
    public List<ResultadoVoltaDto> Voltas { get; set; }
}

public class ResultadoVoltaDto
{
    public int NumeroVolta { get; set; }
    public bool VoltaReconhecimento { get; set; }
    public bool ContaNoTotal { get; set; }
    public decimal TempoVoltaSegundos { get; set; }
    public string TempoVoltaFormatado { get; set; }
    public List<ResultadoEspecialDto> Especiais { get; set; }
}

public class ResultadoEspecialDto
{
    public int NumeroEspecial { get; set; }
    public int NumeroVolta { get; set; }

    // Leituras
    public DateTime? Entrada { get; set; }
    public DateTime? Saida { get; set; }
    public bool TemEntrada { get; set; }
    public bool TemSaida { get; set; }

    // Tempo
    public decimal? TempoSegundos { get; set; }
    public string TempoFormatado { get; set; }

    // Penalidade
    public bool Penalizado { get; set; }
    public string MotivoPenalidade { get; set; }
    public decimal PenalidadeSegundos { get; set; }

    // Ranking
    public int? PosicaoNaEspecial { get; set; }
    public bool MelhorTempoGeral { get; set; }
    public bool MelhorTempoCategoria { get; set; }
}

// ===== CLASSIFICAÇÃO GERAL =====
public class ClassificacaoGeralEnduroDto
{
    public int IdEtapa { get; set; }
    public string NomeEtapa { get; set; }
    public string NomeEvento { get; set; }
    public DateTime DataEtapa { get; set; }

    // Configuração
    public int NumeroVoltas { get; set; }
    public int NumeroEspeciais { get; set; }
    public bool PrimeiraVoltaValida { get; set; }
    public int PenalidadePorFaltaSegundos { get; set; }

    // Estatísticas gerais
    public int TotalInscritos { get; set; }
    public int TotalClassificados { get; set; }
    public int TotalDesclassificados { get; set; }
    public int TotalNaoLargaram { get; set; }
    public int TotalAbandonos { get; set; }
    public int TotalLeituras { get; set; }

    // Melhor tempo
    public string MelhorTempoGeralFormatado { get; set; }
    public string PilotoMelhorTempoGeral { get; set; }

    // Resultados
    public List<ResultadoPilotoEnduroDto> Classificacao { get; set; }

    // Última atualização
    public DateTime DataCalculo { get; set; }
}

// ===== CLASSIFICAÇÃO POR CATEGORIA =====
public class ClassificacaoCategoriaEnduroDto
{
    public int IdCategoria { get; set; }
    public string NomeCategoria { get; set; }
    public int TotalInscritos { get; set; }
    public int TotalClassificados { get; set; }
    public string MelhorTempoFormatado { get; set; }
    public List<ResultadoPilotoEnduroDto> Classificacao { get; set; }
}

// ===== RESUMO RÁPIDO (para exibição em tempo real) =====
public class ResumoClassificacaoDto
{
    public int Posicao { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public string TempoTotal { get; set; }
    public string Diferenca { get; set; }
    public int Penalidades { get; set; }
    public string Status { get; set; }
}

// ===== COMPARATIVO ENTRE PILOTOS =====
public class ComparativoPilotosDto
{
    public ResultadoPilotoEnduroDto Piloto1 { get; set; }
    public ResultadoPilotoEnduroDto Piloto2 { get; set; }
    public decimal DiferencaTotalSegundos { get; set; }
    public string DiferencaTotalFormatado { get; set; }
    public List<ComparativoEspecialDto> ComparativoEspeciais { get; set; }
}

public class ComparativoEspecialDto
{
    public int Volta { get; set; }
    public int Especial { get; set; }
    public decimal? TempoPiloto1 { get; set; }
    public decimal? TempoPiloto2 { get; set; }
    public decimal? DiferencaSegundos { get; set; }
    public string Vantagem { get; set; } // "PILOTO1", "PILOTO2", "EMPATE"
}

// ===== FILTROS =====
public class ResultadoFilterParams
{
    public int? IdCategoria { get; set; }
    public int? TopN { get; set; } // Retornar apenas os N primeiros
    public bool IncluirDesclassificados { get; set; } = false;
    public bool IncluirDetalhes { get; set; } = true;
}

// ===== RANKING DE ESPECIAIS =====
public class RankingEspecialDto
{
    public int NumeroEspecial { get; set; }
    public int NumeroVolta { get; set; }
    public List<TempoEspecialRankingDto> Ranking { get; set; }
}

public class TempoEspecialRankingDto
{
    public int Posicao { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public decimal TempoSegundos { get; set; }
    public string TempoFormatado { get; set; }
    public decimal? DiferencaSegundos { get; set; }
    public string DiferencaFormatado { get; set; }
}