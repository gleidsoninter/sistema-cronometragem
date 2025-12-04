namespace DevNationCrono.API.Models.DTOs;

/// <summary>
/// Notificação de nova passagem
/// </summary>
public class PassagemNotificacaoDto
{
    public string Tipo { get; set; } = "PASSAGEM";
    public DateTime Timestamp { get; set; }
    public int IdEtapa { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public int Volta { get; set; }
    public string TempoVolta { get; set; }
    public int PosicaoAtual { get; set; }
    public int PosicaoCategoria { get; set; }
    public bool MelhorVoltaPessoal { get; set; }
    public bool MelhorVoltaGeral { get; set; }
    public bool MelhorVoltaCategoria { get; set; }
    public string Diferenca { get; set; }
}

/// <summary>
/// Atualização de classificação
/// </summary>
public class ClassificacaoAtualizacaoDto
{
    public string Tipo { get; set; } = "CLASSIFICACAO";
    public DateTime Timestamp { get; set; }
    public int IdEtapa { get; set; }
    public string TipoCronometragem { get; set; }
    public int TotalPilotos { get; set; }
    public int VoltasLider { get; set; }
    public List<PosicaoResumoDto> Top10 { get; set; }
    public MelhorVoltaDto MelhorVoltaGeral { get; set; }
}

public class PosicaoResumoDto
{
    public int Posicao { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public int Voltas { get; set; }
    public string Tempo { get; set; }
    public string Diferenca { get; set; }
    public string UltimaVolta { get; set; }
    public string Status { get; set; }
}

public class MelhorVoltaDto
{
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public string Tempo { get; set; }
    public int Volta { get; set; }
}

/// <summary>
/// Alerta especial (melhor volta, abandono, etc.)
/// </summary>
public class AlertaDto
{
    public string Tipo { get; set; } // MELHOR_VOLTA, ABANDONO, BANDEIRA, etc.
    public DateTime Timestamp { get; set; }
    public int IdEtapa { get; set; }
    public string Mensagem { get; set; }
    public int? NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Detalhe { get; set; }
    public string Prioridade { get; set; } // ALTA, MEDIA, BAIXA
}

/// <summary>
/// Status da prova
/// </summary>
public class StatusProvaDto
{
    public string Tipo { get; set; } = "STATUS_PROVA";
    public int IdEtapa { get; set; }
    public string Status { get; set; } // NAO_INICIADA, EM_ANDAMENTO, BANDEIRA, FINALIZADA
    public DateTime? HoraLargada { get; set; }
    public DateTime? HoraBandeira { get; set; }
    public string TempoDecorrido { get; set; }
    public int? TempoRestanteSegundos { get; set; }
}

/// <summary>
/// Entrada/saída de grupo
/// </summary>
public class GrupoDto
{
    public int? IdEvento { get; set; }
    public int? IdEtapa { get; set; }
    public int? IdCategoria { get; set; }
    public int? NumeroMoto { get; set; }
}
