using DevNationCrono.API.Models.DTOs;

namespace DevNationCrono.API.Services.Interfaces;

public interface IPagamentoService
{
    /// <summary>
    /// Cria uma cobrança PIX para uma inscrição
    /// </summary>
    Task<CobrancaPixResponseDto> CriarCobrancaPixAsync(int idInscricao);

    /// <summary>
    /// Cria uma cobrança PIX para múltiplas inscrições (valor total)
    /// </summary>
    Task<CobrancaPixResponseDto> CriarCobrancaPixMultiplasAsync(List<int> idsInscricoes);

    /// <summary>
    /// Consulta status de uma cobrança
    /// </summary>
    Task<ConsultaPagamentoResponseDto> ConsultarPagamentoAsync(string idExterno);

    /// <summary>
    /// Consulta status pelo ID da inscrição
    /// </summary>
    Task<ConsultaPagamentoResponseDto> ConsultarPagamentoPorInscricaoAsync(int idInscricao);

    /// <summary>
    /// Processa webhook de confirmação de pagamento
    /// </summary>
    Task<bool> ProcessarWebhookAsync(WebhookPagamentoDto webhook);

    /// <summary>
    /// Cancela uma cobrança pendente
    /// </summary>
    Task<bool> CancelarCobrancaAsync(string idExterno);

    /// <summary>
    /// Verifica e atualiza cobranças expiradas
    /// </summary>
    Task<int> VerificarCobrancasExpiradasAsync();

    /// <summary>
    /// Nome do gateway ativo
    /// </summary>
    string GatewayAtivo { get; }
}
