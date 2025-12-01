using DevNationCrono.API.Configuration;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagamento.Asaas;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace DevNationCrono.API.Services.Implementations;

public class AsaasService : IPagamentoService
{
    private readonly HttpClient _httpClient;
    private readonly PagamentoSettings _settings;
    private readonly IInscricaoRepository _inscricaoRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly ILogger<AsaasService> _logger;

    public string GatewayAtivo => "Asaas";

    public AsaasService(
        HttpClient httpClient,
        IOptions<PagamentoSettings> settings,
        IInscricaoRepository inscricaoRepository,
        IPagamentoRepository pagamentoRepository,
        ILogger<AsaasService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _inscricaoRepository = inscricaoRepository;
        _pagamentoRepository = pagamentoRepository;
        _logger = logger;

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri(_settings.Asaas.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("access_token", _settings.Asaas.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<CobrancaPixResponseDto> CriarCobrancaPixAsync(int idInscricao)
    {
        var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(idInscricao);

        if (inscricao == null)
        {
            throw new NotFoundException("Inscrição não encontrada");
        }

        if (inscricao.StatusPagamento == "PAGO")
        {
            throw new ValidationException("Esta inscrição já está paga");
        }

        // Verificar cobrança pendente existente
        var cobrancaExistente = await _pagamentoRepository.GetPendenteByInscricaoAsync(idInscricao);
        if (cobrancaExistente != null && cobrancaExistente.DataExpiracao > DateTime.UtcNow)
        {
            return await ObterQrCodeExistente(cobrancaExistente, inscricao);
        }

        // 1. Criar ou buscar cliente no Asaas
        var idCliente = await ObterOuCriarClienteAsync(inscricao.Piloto);

        // 2. Criar cobrança
        var dataVencimento = DateTime.UtcNow.AddMinutes(_settings.Asaas.ExpiracaoMinutos);

        var cobrancaRequest = new AsaasCobrancaRequest
        {
            Customer = idCliente,
            BillingType = "PIX",
            Value = inscricao.ValorFinal,
            DueDate = dataVencimento.ToString("yyyy-MM-dd"),
            Description = $"Inscrição {inscricao.Evento.Nome} - {inscricao.Categoria.Nome}",
            ExternalReference = $"INSC-{idInscricao}"
        };

        var json = JsonConvert.SerializeObject(cobrancaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Criando cobrança PIX no Asaas: {Request}", json);

        var response = await _httpClient.PostAsync("/payments", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro ao criar cobrança no Asaas: {Status} - {Response}",
                response.StatusCode, responseContent);

            var error = JsonConvert.DeserializeObject<AsaasError>(responseContent);
            var mensagem = error?.Errors?.FirstOrDefault()?.Description ?? responseContent;
            throw new ExternalServiceException($"Erro no Asaas: {mensagem}");
        }

        var cobrancaResponse = JsonConvert.DeserializeObject<AsaasCobrancaResponse>(responseContent);

        // 3. Buscar QR Code PIX
        var qrCodeResponse = await ObterQrCodeAsync(cobrancaResponse.Id);

        _logger.LogInformation("Cobrança PIX criada no Asaas: ID {Id}, Status {Status}",
            cobrancaResponse.Id, cobrancaResponse.Status);

        // 4. Salvar registro de pagamento
        var pagamento = new Pagamento
        {
            IdInscricao = idInscricao,
            IdExterno = cobrancaResponse.Id,
            Gateway = "Asaas",
            Valor = inscricao.ValorFinal,
            Status = MapearStatusAsaas(cobrancaResponse.Status),
            QrCode = qrCodeResponse.Payload,
            QrCodeBase64 = qrCodeResponse.EncodedImage,
            CopiaCola = qrCodeResponse.Payload,
            DataCriacao = DateTime.UtcNow,
            DataExpiracao = qrCodeResponse.ExpirationDate,
            PayloadOriginal = responseContent
        };

        await _pagamentoRepository.AddAsync(pagamento);

        // 5. Atualizar inscrição
        inscricao.StatusPagamento = "AGUARDANDO_PAGAMENTO";
        inscricao.QrCodePix = pagamento.QrCode;
        inscricao.CodigoPix = pagamento.CopiaCola;
        await _inscricaoRepository.UpdateAsync(inscricao);

        return new CobrancaPixResponseDto
        {
            IdCobranca = pagamento.Id.ToString(),
            IdExterno = cobrancaResponse.Id,
            Valor = inscricao.ValorFinal,
            Status = pagamento.Status,
            QrCode = pagamento.QrCode,
            QrCodeBase64 = pagamento.QrCodeBase64,
            QrCodeUrl = cobrancaResponse.InvoiceUrl,
            CopiaCola = pagamento.CopiaCola,
            DataCriacao = pagamento.DataCriacao,
            DataExpiracao = pagamento.DataExpiracao,
            Gateway = "Asaas",
            IdInscricao = idInscricao,
            NomePiloto = inscricao.Piloto.Nome,
            NomeEvento = inscricao.Evento.Nome,
            Descricao = $"Inscrição {inscricao.Categoria.Nome} - Moto #{inscricao.NumeroMoto}"
        };
    }

    public async Task<CobrancaPixResponseDto> CriarCobrancaPixMultiplasAsync(List<int> idsInscricoes)
    {
        // Similar ao MercadoPago, mas usando Asaas
        // Implementação análoga...

        if (idsInscricoes == null || !idsInscricoes.Any())
        {
            throw new ValidationException("Informe pelo menos uma inscrição");
        }

        var inscricoes = new List<Inscricao>();
        decimal valorTotal = 0;
        int? idPiloto = null;

        foreach (var id in idsInscricoes)
        {
            var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);
            if (inscricao == null) throw new NotFoundException($"Inscrição {id} não encontrada");
            if (inscricao.StatusPagamento == "PAGO") throw new ValidationException($"Inscrição {id} já está paga");

            if (idPiloto == null) idPiloto = inscricao.IdPiloto;
            else if (inscricao.IdPiloto != idPiloto)
                throw new ValidationException("Todas inscrições devem ser do mesmo piloto");

            inscricoes.Add(inscricao);
            valorTotal += inscricao.ValorFinal;
        }

        var primeiraInscricao = inscricoes.First();
        var categorias = string.Join(", ", inscricoes.Select(i => i.Categoria.Nome));

        // Criar cliente
        var idCliente = await ObterOuCriarClienteAsync(primeiraInscricao.Piloto);

        // Criar cobrança única
        var dataVencimento = DateTime.UtcNow.AddMinutes(_settings.Asaas.ExpiracaoMinutos);

        var cobrancaRequest = new AsaasCobrancaRequest
        {
            Customer = idCliente,
            BillingType = "PIX",
            Value = valorTotal,
            DueDate = dataVencimento.ToString("yyyy-MM-dd"),
            Description = $"Inscrições {primeiraInscricao.Evento.Nome} - {categorias}",
            ExternalReference = $"MULTI-{string.Join("-", idsInscricoes)}"
        };

        var json = JsonConvert.SerializeObject(cobrancaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/payments", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonConvert.DeserializeObject<AsaasError>(responseContent);
            throw new ExternalServiceException($"Erro no Asaas: {error?.Errors?.FirstOrDefault()?.Description}");
        }

        var cobrancaResponse = JsonConvert.DeserializeObject<AsaasCobrancaResponse>(responseContent);
        var qrCodeResponse = await ObterQrCodeAsync(cobrancaResponse.Id);

        // Salvar para cada inscrição
        foreach (var inscricao in inscricoes)
        {
            var pagamento = new Pagamento
            {
                IdInscricao = inscricao.Id,
                IdExterno = cobrancaResponse.Id,
                Gateway = "Asaas",
                Valor = inscricao.ValorFinal,
                Status = MapearStatusAsaas(cobrancaResponse.Status),
                QrCode = qrCodeResponse.Payload,
                QrCodeBase64 = qrCodeResponse.EncodedImage,
                CopiaCola = qrCodeResponse.Payload,
                DataCriacao = DateTime.UtcNow,
                DataExpiracao = qrCodeResponse.ExpirationDate,
                PayloadOriginal = responseContent
            };

            await _pagamentoRepository.AddAsync(pagamento);

            inscricao.StatusPagamento = "AGUARDANDO_PAGAMENTO";
            inscricao.QrCodePix = pagamento.QrCode;
            await _inscricaoRepository.UpdateAsync(inscricao);
        }

        return new CobrancaPixResponseDto
        {
            IdCobranca = cobrancaResponse.Id,
            IdExterno = cobrancaResponse.Id,
            Valor = valorTotal,
            Status = MapearStatusAsaas(cobrancaResponse.Status),
            QrCode = qrCodeResponse.Payload,
            QrCodeBase64 = qrCodeResponse.EncodedImage,
            QrCodeUrl = cobrancaResponse.InvoiceUrl,
            CopiaCola = qrCodeResponse.Payload,
            DataCriacao = DateTime.UtcNow,
            DataExpiracao = qrCodeResponse.ExpirationDate,
            Gateway = "Asaas",
            IdInscricao = primeiraInscricao.Id,
            NomePiloto = primeiraInscricao.Piloto.Nome,
            NomeEvento = primeiraInscricao.Evento.Nome,
            Descricao = $"Inscrições: {categorias}"
        };
    }

    public async Task<ConsultaPagamentoResponseDto> ConsultarPagamentoAsync(string idExterno)
    {
        var response = await _httpClient.GetAsync($"/payments/{idExterno}");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceException("Erro ao consultar pagamento no Asaas");
        }

        var cobranca = JsonConvert.DeserializeObject<AsaasCobrancaResponse>(responseContent);

        return new ConsultaPagamentoResponseDto
        {
            IdCobranca = cobranca.Id,
            IdExterno = cobranca.Id,
            Valor = cobranca.Value,
            ValorPago = cobranca.Status == "RECEIVED" ? cobranca.NetValue : null,
            Status = MapearStatusAsaas(cobranca.Status),
            TransacaoId = cobranca.Id
        };
    }

    public async Task<ConsultaPagamentoResponseDto> ConsultarPagamentoPorInscricaoAsync(int idInscricao)
    {
        var pagamento = await _pagamentoRepository.GetUltimoByInscricaoAsync(idInscricao);
        if (pagamento == null)
            throw new NotFoundException("Nenhum pagamento encontrado");

        return await ConsultarPagamentoAsync(pagamento.IdExterno);
    }

    public async Task<bool> ProcessarWebhookAsync(WebhookPagamentoDto webhook)
    {
        _logger.LogInformation("Processando webhook Asaas: {IdExterno}, Status: {Status}",
            webhook.IdExterno, webhook.Status);

        var pagamentos = await _pagamentoRepository.GetByIdExternoAsync(webhook.IdExterno);
        if (!pagamentos.Any())
        {
            _logger.LogWarning("Pagamento não encontrado: {IdExterno}", webhook.IdExterno);
            return false;
        }

        var novoStatus = MapearStatusAsaas(webhook.Status);

        foreach (var pagamento in pagamentos)
        {
            pagamento.Status = novoStatus;
            pagamento.DataAtualizacao = DateTime.UtcNow;

            if (novoStatus == StatusPagamentoPix.Pago)
            {
                pagamento.DataPagamento = webhook.DataPagamento ?? DateTime.UtcNow;
                pagamento.TransacaoId = webhook.TransacaoId;
            }

            await _pagamentoRepository.UpdateAsync(pagamento);

            var inscricao = await _inscricaoRepository.GetByIdAsync(pagamento.IdInscricao);
            if (inscricao != null)
            {
                if (novoStatus == StatusPagamentoPix.Pago)
                {
                    inscricao.StatusPagamento = "PAGO";
                    inscricao.MetodoPagamento = "PIX";
                    inscricao.TransacaoId = webhook.TransacaoId;
                    inscricao.DataPagamento = webhook.DataPagamento ?? DateTime.UtcNow;
                }
                else if (novoStatus == StatusPagamentoPix.Expirado)
                {
                    inscricao.StatusPagamento = "PENDENTE";
                }

                await _inscricaoRepository.UpdateAsync(inscricao);
            }
        }

        return true;
    }

    public async Task<bool> CancelarCobrancaAsync(string idExterno)
    {
        var response = await _httpClient.DeleteAsync($"/payments/{idExterno}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro ao cancelar cobrança Asaas: {Id}", idExterno);
            return false;
        }

        var pagamentos = await _pagamentoRepository.GetByIdExternoAsync(idExterno);
        foreach (var pagamento in pagamentos)
        {
            pagamento.Status = StatusPagamentoPix.Cancelado;
            await _pagamentoRepository.UpdateAsync(pagamento);
        }

        return true;
    }

    public async Task<int> VerificarCobrancasExpiradasAsync()
    {
        var expiradas = await _pagamentoRepository.GetExpiradasAsync();
        var count = 0;

        foreach (var pagamento in expiradas)
        {
            try
            {
                var status = await ConsultarPagamentoAsync(pagamento.IdExterno);

                if (status.Status == StatusPagamentoPix.Pago)
                {
                    await ProcessarWebhookAsync(new WebhookPagamentoDto
                    {
                        IdExterno = pagamento.IdExterno,
                        Status = "RECEIVED",
                        DataPagamento = status.DataPagamento,
                        Gateway = "Asaas"
                    });
                }
                else
                {
                    pagamento.Status = StatusPagamentoPix.Expirado;
                    await _pagamentoRepository.UpdateAsync(pagamento);
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar cobrança: {Id}", pagamento.Id);
            }
        }

        return count;
    }

    #region Métodos Privados

    private async Task<string> ObterOuCriarClienteAsync(Piloto piloto)
    {
        // Buscar cliente existente por CPF
        var response = await _httpClient.GetAsync($"/customers?cpfCnpj={piloto.Cpf}");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var resultado = JsonConvert.DeserializeObject<AsaasListaResponse<AsaasClienteResponse>>(responseContent);
            if (resultado?.Data?.Any() == true)
            {
                return resultado.Data.First().Id;
            }
        }

        // Criar novo cliente
        var clienteRequest = new AsaasClienteRequest
        {
            Name = piloto.Nome,
            Email = piloto.Email,
            CpfCnpj = piloto.Cpf,
            MobilePhone = piloto.Telefone?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
            ExternalReference = $"PILOTO-{piloto.Id}"
        };

        var json = JsonConvert.SerializeObject(clienteRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        response = await _httpClient.PostAsync("/customers", content);
        responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonConvert.DeserializeObject<AsaasError>(responseContent);
            throw new ExternalServiceException($"Erro ao criar cliente no Asaas: {error?.Errors?.FirstOrDefault()?.Description}");
        }

        var cliente = JsonConvert.DeserializeObject<AsaasClienteResponse>(responseContent);
        return cliente.Id;
    }

    private async Task<AsaasPixQrCodeResponse> ObterQrCodeAsync(string idCobranca)
    {
        var response = await _httpClient.GetAsync($"/payments/{idCobranca}/pixQrCode");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceException("Erro ao obter QR Code do Asaas");
        }

        return JsonConvert.DeserializeObject<AsaasPixQrCodeResponse>(responseContent);
    }

    private async Task<CobrancaPixResponseDto> ObterQrCodeExistente(Pagamento pagamento, Inscricao inscricao)
    {
        // Tentar obter QR Code atualizado
        try
        {
            var qrCode = await ObterQrCodeAsync(pagamento.IdExterno);
            pagamento.QrCode = qrCode.Payload;
            pagamento.QrCodeBase64 = qrCode.EncodedImage;
            await _pagamentoRepository.UpdateAsync(pagamento);
        }
        catch { /* Usar dados existentes */ }

        return new CobrancaPixResponseDto
        {
            IdCobranca = pagamento.Id.ToString(),
            IdExterno = pagamento.IdExterno,
            Valor = pagamento.Valor,
            Status = pagamento.Status,
            QrCode = pagamento.QrCode,
            QrCodeBase64 = pagamento.QrCodeBase64,
            CopiaCola = pagamento.CopiaCola,
            DataCriacao = pagamento.DataCriacao,
            DataExpiracao = pagamento.DataExpiracao,
            Gateway = "Asaas",
            IdInscricao = inscricao.Id,
            NomePiloto = inscricao.Piloto?.Nome ?? "",
            NomeEvento = inscricao.Evento?.Nome ?? "",
            Descricao = $"Inscrição {inscricao.Categoria?.Nome}"
        };
    }

    private string MapearStatusAsaas(string statusAsaas)
    {
        return statusAsaas?.ToUpper() switch
        {
            "PENDING" => StatusPagamentoPix.Aguardando,
            "RECEIVED" => StatusPagamentoPix.Pago,
            "CONFIRMED" => StatusPagamentoPix.Pago,
            "OVERDUE" => StatusPagamentoPix.Expirado,
            "REFUNDED" => StatusPagamentoPix.Reembolsado,
            "RECEIVED_IN_CASH" => StatusPagamentoPix.Pago,
            "REFUND_REQUESTED" => StatusPagamentoPix.Processando,
            "CHARGEBACK_REQUESTED" => StatusPagamentoPix.Processando,
            "CHARGEBACK_DISPUTE" => StatusPagamentoPix.Processando,
            "AWAITING_CHARGEBACK_REVERSAL" => StatusPagamentoPix.Processando,
            "DUNNING_REQUESTED" => StatusPagamentoPix.Pendente,
            "DUNNING_RECEIVED" => StatusPagamentoPix.Pago,
            "AWAITING_RISK_ANALYSIS" => StatusPagamentoPix.Processando,
            _ => StatusPagamentoPix.Pendente
        };
    }

    #endregion
}

// Classe auxiliar para lista do Asaas
public class AsaasListaResponse<T>
{
    [JsonProperty("data")]
    public List<T> Data { get; set; }

    [JsonProperty("hasMore")]
    public bool HasMore { get; set; }

    [JsonProperty("totalCount")]
    public int TotalCount { get; set; }
}
