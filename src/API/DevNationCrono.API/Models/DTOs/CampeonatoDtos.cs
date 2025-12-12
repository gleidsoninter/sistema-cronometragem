using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

// ==================== CAMPEONATO ====================

public class CampeonatoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Sigla { get; set; }
    public int Ano { get; set; }
    public string? Descricao { get; set; }
    public string? Regulamento { get; set; }
    public string? ImagemBanner { get; set; }
    public int IdModalidade { get; set; }
    public string NomeModalidade { get; set; } = string.Empty;
    public string TipoCronometragem { get; set; } = string.Empty;
    public int? QtdeEtapasValidas { get; set; }

    // Regras de pontuação - Circuito
    public decimal? PercentualMinimoVoltasLider { get; set; }
    public bool ExigeBandeirada { get; set; }

    // Regras de pontuação - Enduro
    public decimal? PercentualMinimoProvaEnduro { get; set; }

    // Regras de pontuação - Gerais
    public bool TodosParticipantesPontuam { get; set; }
    public bool DesclassificadoNaoPontua { get; set; }
    public bool AbandonoNaoPontua { get; set; }

    public string Status { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    // Calculados
    public int TotalEventos { get; set; }
    public int TotalInscritos { get; set; }
    public List<CampeonatoPontuacaoDto> Pontuacoes { get; set; } = new();
}

public class CampeonatoResumoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Sigla { get; set; }
    public int Ano { get; set; }
    public int IdModalidade { get; set; }
    public string NomeModalidade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalEventos { get; set; }
    public int TotalInscritos { get; set; }
}

public class CampeonatoCreateDto
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Nome deve ter entre 5 e 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Sigla { get; set; }

    [Required(ErrorMessage = "Ano é obrigatório")]
    [Range(2020, 2100, ErrorMessage = "Ano inválido")]
    public int Ano { get; set; }

    [StringLength(2000)]
    public string? Descricao { get; set; }

    [StringLength(5000)]
    public string? Regulamento { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "URL da imagem inválida")]
    public string? ImagemBanner { get; set; }

    [Required(ErrorMessage = "Modalidade é obrigatória")]
    public int IdModalidade { get; set; }

    [Range(1, 50, ErrorMessage = "Quantidade deve estar entre 1 e 50")]
    public int? QtdeEtapasValidas { get; set; }

    // Regras de pontuação - Circuito
    [Range(0, 1, ErrorMessage = "Percentual deve estar entre 0 e 1 (ex: 0.50 para 50%)")]
    public decimal? PercentualMinimoVoltasLider { get; set; }

    public bool ExigeBandeirada { get; set; }

    // Regras de pontuação - Enduro
    [Range(0, 1, ErrorMessage = "Percentual deve estar entre 0 e 1 (ex: 0.50 para 50%)")]
    public decimal? PercentualMinimoProvaEnduro { get; set; }

    // Regras de pontuação - Gerais
    public bool TodosParticipantesPontuam { get; set; }
    public bool DesclassificadoNaoPontua { get; set; } = true;
    public bool AbandonoNaoPontua { get; set; } = true;

    public List<CampeonatoPontuacaoCreateDto>? Pontuacoes { get; set; }
}

public class CampeonatoUpdateDto
{
    [StringLength(200, MinimumLength = 5)]
    public string? Nome { get; set; }

    [StringLength(20)]
    public string? Sigla { get; set; }

    [Range(2020, 2100)]
    public int? Ano { get; set; }

    [StringLength(2000)]
    public string? Descricao { get; set; }

    [StringLength(5000)]
    public string? Regulamento { get; set; }

    [StringLength(500)]
    public string? ImagemBanner { get; set; }

    [Range(1, 50)]
    public int? QtdeEtapasValidas { get; set; }

    // Regras de pontuação - Circuito
    [Range(0, 1)]
    public decimal? PercentualMinimoVoltasLider { get; set; }

    public bool? ExigeBandeirada { get; set; }

    // Regras de pontuação - Enduro
    [Range(0, 1)]
    public decimal? PercentualMinimoProvaEnduro { get; set; }

    // Regras de pontuação - Gerais
    public bool? TodosParticipantesPontuam { get; set; }
    public bool? DesclassificadoNaoPontua { get; set; }
    public bool? AbandonoNaoPontua { get; set; }

    public string? Status { get; set; }
    public bool? Ativo { get; set; }
}

// ==================== PONTUAÇÃO ====================

public class CampeonatoPontuacaoDto
{
    public int Id { get; set; }
    public int IdCampeonato { get; set; }
    public int Posicao { get; set; }
    public decimal Pontos { get; set; }
}

public class CampeonatoPontuacaoCreateDto
{
    [Required(ErrorMessage = "Posição é obrigatória")]
    [Range(1, 100, ErrorMessage = "Posição deve estar entre 1 e 100")]
    public int Posicao { get; set; }

    [Required(ErrorMessage = "Pontos é obrigatório")]
    [Range(0, 1000, ErrorMessage = "Pontos deve estar entre 0 e 1000")]
    public decimal Pontos { get; set; }
}

// ==================== CLASSIFICAÇÃO DO CAMPEONATO ====================

public class ClassificacaoCampeonatoDto
{
    public int IdCampeonato { get; set; }
    public string NomeCampeonato { get; set; } = string.Empty;
    public string? Sigla { get; set; }
    public int Ano { get; set; }
    public string NomeModalidade { get; set; } = string.Empty;
    public int TotalEventos { get; set; }
    public int EventosRealizados { get; set; }
    public int? QtdeEtapasValidas { get; set; }
    public List<ClassificacaoCategoriaCampeonatoDto> Categorias { get; set; } = new();
}

public class ClassificacaoCategoriaCampeonatoDto
{
    public int IdCategoria { get; set; }
    public string NomeCategoria { get; set; } = string.Empty;
    public int TotalPilotos { get; set; }
    public List<PilotoCampeonatoDto> Pilotos { get; set; } = new();
}

public class PilotoCampeonatoDto
{
    public int Posicao { get; set; }
    public int IdPiloto { get; set; }
    public string NomePiloto { get; set; } = string.Empty;
    public int NumeroMoto { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Equipe { get; set; }
    public decimal TotalPontos { get; set; }
    public int EtapasDisputadas { get; set; }
    public int Vitorias { get; set; }
    public int Podios { get; set; }
    public int? MelhorPosicao { get; set; }
    public List<PontuacaoEventoDto> PontosPorEvento { get; set; } = new();
}

public class PontuacaoEventoDto
{
    public int IdEvento { get; set; }
    public string NomeEvento { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public string Local { get; set; } = string.Empty;
    public int? Posicao { get; set; }
    public decimal Pontos { get; set; }
    /// <summary>
    /// Status: CLASSIFICADO, DNS, DNF, DSQ
    /// </summary>
    public string Status { get; set; } = string.Empty;
    public bool Descartado { get; set; }
}
