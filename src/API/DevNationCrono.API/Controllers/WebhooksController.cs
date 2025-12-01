using DevNationCrono.API.Configuration;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Pagamento.Asaas;
using DevNationCrono.API.Models.Pagamento.MercadoPago;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPagamentoService _pagamentoService;
    private readonly PagamentoSettings _settings;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPagamentoService pagamentoService,
        IOptions<PagamentoSettings> settings,
        ILogger<WebhooksController> logger)
    {
        _pagamentoService = pagamentoService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Webhook do Mercado Pago
    /// </summary>
    [HttpPost("mercadopago")]
    public async Task<IActionResult> MercadoPago()
    {
        try
        {
            // Ler body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("Webhook Mercado Pago recebido: {Body}", body);

            // Verificar assinatura (opcional, mas recomendado)
            if (!ValidarAssinaturaMercadoPago(body))
            {
                _logger.LogWarning("Assinatura inválida no webhook do Mercado Pago");
                // Em produção, retornaria 401
            }

            var webhook = JsonConvert.DeserializeObject<MercadoPagoWebhook>(body);

            // Processar apenas eventos de pagamento
            if (webhook.Type != "payment")
            {
                _logger.LogInformation("Evento ignorado: {Type}", webhook.Type);
                return Ok();
            }

            // Buscar detalhes do pagamento no MP
            var idPagamento = webhook.Data.Id;

            // Aqui você poderia consultar o MP para obter status atual
            // Por simplicidade, vamos mapear baseado no action
            var status = webhook.Action switch
            {
                "payment.created" => "pending",
                "payment.updated" => "pending", // Precisa consultar para saber o status real
                _ => "pending"
            };

            var webhookDto = new WebhookPagamentoDto
            {
                IdExterno = idPagamento,
                Status = status,
                Gateway = "MercadoPago",
                PayloadOriginal = body
            };

            // Se for confirmação, buscar dados completos
            // Na prática, o ideal é consultar /v1/payments/{id}

            await _pagamentoService.ProcessarWebhookAsync(webhookDto);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook do Mercado Pago");
            return Ok(); // Retorna OK para não reenviar
        }
    }

    /// <summary>
    /// Webhook do Asaas
    /// </summary>
    [HttpPost("asaas")]
    public async Task<IActionResult> Asaas()
    {
        try
        {
            // Verificar token de autenticação
            var token = Request.Headers["asaas-access-token"].FirstOrDefault();
            if (token != _settings.Asaas.WebhookToken)
            {
                _logger.LogWarning("Token inválido no webhook do Asaas");
                return Unauthorized();
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("Webhook Asaas recebido: {Body}", body);

            var webhook = JsonConvert.DeserializeObject<AsaasWebhook>(body);

            // Eventos de pagamento PIX
            var eventosValidos = new[]
            {
                    "PAYMENT_RECEIVED",
                    "PAYMENT_CONFIRMED",
                    "PAYMENT_OVERDUE",
                    "PAYMENT_DELETED",
                    "PAYMENT_REFUNDED"
                };

            if (!eventosValidos.Contains(webhook.Event))
            {
                _logger.LogInformation("Evento Asaas ignorado: {Event}", webhook.Event);
                return Ok();
            }

            DateTime? dataPagamento = null;
            if (!string.IsNullOrEmpty(webhook.Payment.PaymentDate))
            {
                DateTime.TryParse(webhook.Payment.PaymentDate, out var dt);
                dataPagamento = dt;
            }

            var webhookDto = new WebhookPagamentoDto
            {
                IdExterno = webhook.Payment.Id,
                Status = webhook.Payment.Status,
                Valor = webhook.Payment.Value,
                DataPagamento = dataPagamento,
                TransacaoId = webhook.Payment.Id,
                Gateway = "Asaas",
                PayloadOriginal = body
            };

            await _pagamentoService.ProcessarWebhookAsync(webhookDto);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook do Asaas");
            return Ok();
        }
    }

    /// <summary>
    /// Endpoint de teste para simular webhook
    /// </summary>
    [HttpPost("teste")]
    public async Task<IActionResult> Teste([FromBody] WebhookPagamentoDto dto)
    {
        if (!_settings.MercadoPago.Sandbox && !_settings.Asaas.Sandbox)
        {
            return BadRequest("Endpoint disponível apenas em ambiente de teste");
        }

        _logger.LogInformation("Webhook de teste recebido: {IdExterno}", dto.IdExterno);

        await _pagamentoService.ProcessarWebhookAsync(dto);

        return Ok(new { mensagem = "Webhook processado com sucesso" });
    }

    private bool ValidarAssinaturaMercadoPago(string body)
    {
        // Implementar validação HMAC se necessário
        // Por enquanto, retorna true
        return true;
    }
}
