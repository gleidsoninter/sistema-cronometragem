namespace DevNationCrono.API.Models.DTOs;

public class InscricaoDto
{
    public int Id { get; set; }

    // Piloto
    public int IdPiloto { get; set; }
    public string NomePiloto { get; set; }
    public string CpfPiloto { get; set; }
    public string? TelefonePiloto { get; set; }

    // Evento
    public int IdEvento { get; set; }
    public string NomeEvento { get; set; }
    public DateTime DataEvento { get; set; }

    // Categoria
    public int IdCategoria { get; set; }
    public string NomeCategoria { get; set; }

    // Etapa
    public int IdEtapa { get; set; }
    public string NomeEtapa { get; set; }
    public int NumeroEtapa { get; set; }

    // Inscrição
    public int NumeroMoto { get; set; }
    public decimal ValorOriginal { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal ValorFinal { get; set; }

    // Pagamento
    public string StatusPagamento { get; set; }
    public string? MetodoPagamento { get; set; }
    public string? TransacaoId { get; set; }
    public DateTime? DataPagamento { get; set; }

    public DateTime DataInscricao { get; set; }
    public bool Ativo { get; set; }
    public string? Observacoes { get; set; }
}
