namespace DevNationCrono.API.Models.DTOs;

public class ResultadoPilotoCircuitoDto
{
    // Posições
    public int PosicaoGeral { get; set; }
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

    // Voltas
    public int TotalVoltas { get; set; }
    public int VoltasCompletadas { get; set; }

    // Tempo total (desde largada até última passagem)
    public decimal TempoTotalSegundos { get; set; }
    public string TempoTotalFormatado { get; set; }

    // Diferenças
    public int DiferencaVoltasLider { get; set; }
    public decimal? DiferencaTempoLiderSegundos { get; set; }
    public string DiferencaLiderFormatado { get; set; }
    public string DiferencaAnteriorFormatado { get; set; }

    // Melhor volta
    public int? VoltaMelhorTempo { get; set; }
    public decimal? MelhorVoltaSegundos { get; set; }
    public string MelhorVoltaFormatado { get; set; }
    public bool MelhorVoltaGeral { get; set; }
    public bool MelhorVoltaCategoria { get; set; }

    // Última volta
    public decimal? UltimaVoltaSegundos { get; set; }
    public string UltimaVoltaFormatado { get; set; }

    // Média
    public decimal? MediaVoltaSegundos { get; set; }
    public string MediaVoltaFormatado { get; set; }

    // Status
    public string Status { get; set; } // CORRENDO, FINALIZADO, NAO_LARGOU, ABANDONO, DESCLASSIFICADO
    public string MotivoStatus { get; set; }
    public bool EmPista { get; set; }

    // Última atualização
    public DateTime? UltimaPassagem { get; set; }

    // Detalhamento de voltas
    public List<VoltaDetalheDto> Voltas { get; set; }
}

public class VoltaDetalheDto
{
    public int NumeroVolta { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal TempoVoltaSegundos { get; set; }
    public string TempoVoltaFormatado { get; set; }
    public decimal TempoAcumuladoSegundos { get; set; }
    public string TempoAcumuladoFormatado { get; set; }
    public bool MelhorVolta { get; set; }
    public int? PosicaoNaVolta { get; set; }
    public decimal? DiferencaMelhorSegundos { get; set; }
}

// ===== CLASSIFICAÇÃO GERAL =====
public class ClassificacaoGeralCircuitoDto
{
    public int IdEtapa { get; set; }
    public string NomeEtapa { get; set; }
    public string NomeEvento { get; set; }
    public DateTime DataEtapa { get; set; }

    // Configuração
    public int? TempoProvaMinutos { get; set; }
    public int? NumeroVoltasPrevistas { get; set; }

    // Status da prova
    public string StatusProva { get; set; } // NAO_INICIADA, EM_ANDAMENTO, BANDEIRA, FINALIZADA
    public DateTime? HoraLargada { get; set; }
    public DateTime? HoraBandeira { get; set; }
    public TimeSpan? TempoDecorrido { get; set; }
    public int VoltasLider { get; set; }

    // Estatísticas
    public int TotalInscritos { get; set; }
    public int TotalEmPista { get; set; }
    public int TotalFinalizados { get; set; }
    public int TotalAbandonos { get; set; }
    public int TotalPassagens { get; set; }

    // Melhor volta geral
    public string MelhorVoltaGeralFormatado { get; set; }
    public int? MotoMelhorVoltaGeral { get; set; }
    public string PilotoMelhorVoltaGeral { get; set; }

    // Categorias na pista
    public List<string> CategoriasEmPista { get; set; }

    // Classificação
    public List<ResultadoPilotoCircuitoDto> Classificacao { get; set; }

    // Última atualização
    public DateTime DataCalculo { get; set; }
}

// ===== CLASSIFICAÇÃO POR CATEGORIA =====
public class ClassificacaoCategoriaCircuitoDto
{
    public int IdCategoria { get; set; }
    public string NomeCategoria { get; set; }
    public int TotalInscritos { get; set; }
    public int TotalEmPista { get; set; }
    public int TotalFinalizados { get; set; }
    public int VoltasLiderCategoria { get; set; }
    public string MelhorVoltaCategoriaFormatado { get; set; }
    public string PilotoMelhorVoltaCategoria { get; set; }
    public List<ResultadoPilotoCircuitoDto> Classificacao { get; set; }
}

// ===== RESUMO TEMPO REAL (leve, para atualização frequente) =====
public class ResumoTempoRealDto
{
    public int PosicaoGeral { get; set; }
    public int PosicaoCategoria { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public int Voltas { get; set; }
    public string TempoTotal { get; set; }
    public string Diferenca { get; set; }
    public string UltimaVolta { get; set; }
    public string MelhorVolta { get; set; }
    public string Status { get; set; }
    public bool EmPista { get; set; }
}

// ===== ANÁLISE DE DESEMPENHO =====
public class AnaliseDesempenhoDto
{
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }

    // Tempos
    public decimal MelhorVoltaSegundos { get; set; }
    public decimal PiorVoltaSegundos { get; set; }
    public decimal MediaVoltaSegundos { get; set; }
    public decimal DesvioPadrao { get; set; }

    // Formatados
    public string MelhorVoltaFormatado { get; set; }
    public string PiorVoltaFormatado { get; set; }
    public string MediaFormatado { get; set; }

    // Consistência (quanto menor, mais consistente)
    public decimal IndiceConsistencia { get; set; }
    public string ClassificacaoConsistencia { get; set; } // EXCELENTE, BOM, REGULAR, IRREGULAR

    // Evolução
    public List<decimal> TemposPorVolta { get; set; }
    public string Tendencia { get; set; } // MELHORANDO, ESTAVEL, PIORANDO

    // Comparação com campo
    public decimal? DiferencaMediaCampo { get; set; }
    public int RankingMelhorVolta { get; set; }
}

// ===== HISTÓRICO DE PASSAGENS (para tela ao vivo) =====
public class PassagemRecente
{
    public DateTime Timestamp { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public int Volta { get; set; }
    public string TempoVolta { get; set; }
    public int PosicaoAtual { get; set; }
    public bool MelhorVoltaPessoal { get; set; }
    public bool MelhorVoltaGeral { get; set; }
}

// ===== CONTROLE DE PROVA =====
public class ControleProvaDto
{
    public int IdEtapa { get; set; }
    public string StatusAtual { get; set; }
    public DateTime? HoraLargada { get; set; }
    public DateTime? HoraBandeira { get; set; }
    public int? TempoProvaMinutos { get; set; }
    public int? VoltasRestantes { get; set; }
}

public class IniciarProvaDto
{
    public int IdEtapa { get; set; }
    public DateTime? HoraLargada { get; set; } // Se null, usa hora atual
}

public class EncerrarProvaDto
{
    public int IdEtapa { get; set; }
    public DateTime? HoraBandeira { get; set; }
}
