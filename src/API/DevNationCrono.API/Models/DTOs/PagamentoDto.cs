using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class CriarCobrancaPixDto
{
    [Required]
    public int IdInscricao { get; set; }

    /// <summary>
    /// Se informado, gera cobrança para múltiplas inscrições
    /// </summary>
    public List<int>? IdsInscricoes { get; set; }
}

// ===== RESPOSTA DA COBRANÇA =====
public class CobrancaPixResponseDto
{
    public string IdCobranca { get; set; }
    public string IdExterno { get; set; } // ID no gateway
    public decimal Valor { get; set; }
    public string Status { get; set; }
    public string QrCode { get; set; } // Payload EMV
    public string QrCodeBase64 { get; set; } // Imagem em Base64
    public string QrCodeUrl { get; set; } // URL da imagem (se disponível)
    public string CopiaCola { get; set; } // Código copia e cola
    public DateTime DataCriacao { get; set; }
    public DateTime DataExpiracao { get; set; }
    public string Gateway { get; set; } // MercadoPago ou Asaas

    // Dados da inscrição
    public int IdInscricao { get; set; }
    public string NomePiloto { get; set; }
    public string NomeEvento { get; set; }
    public string Descricao { get; set; }
}

// ===== CONSULTA DE PAGAMENTO =====
public class ConsultaPagamentoResponseDto
{
    public string IdCobranca { get; set; }
    public string IdExterno { get; set; }
    public decimal Valor { get; set; }
    public decimal? ValorPago { get; set; }
    public string Status { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string? TransacaoId { get; set; }
    public string? PagadorNome { get; set; }
    public string? PagadorDocumento { get; set; }
}

// ===== STATUS POSSÍVEIS =====
public static class StatusPagamentoPix
{
    public const string Pendente = "PENDENTE";
    public const string Aguardando = "AGUARDANDO_PAGAMENTO";
    public const string Processando = "PROCESSANDO";
    public const string Pago = "PAGO";
    public const string Expirado = "EXPIRADO";
    public const string Cancelado = "CANCELADO";
    public const string Reembolsado = "REEMBOLSADO";
    public const string Erro = "ERRO";
}

// ===== WEBHOOK PAYLOAD (genérico) =====
public class WebhookPagamentoDto
{
    public string IdExterno { get; set; }
    public string Status { get; set; }
    public decimal? Valor { get; set; }
    public DateTime? DataPagamento { get; set; }
    public string? TransacaoId { get; set; }
    public string Gateway { get; set; }
    public string PayloadOriginal { get; set; }
}
