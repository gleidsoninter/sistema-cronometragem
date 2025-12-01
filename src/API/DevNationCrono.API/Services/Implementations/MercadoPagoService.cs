using DevNationCrono.API.Configuration;
using DevNationCrono.API.Exceptions;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagamento.MercadoPago;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace DevNationCrono.API.Services.Implementations;

public class MercadoPagoService : IPagamentoService
{
    private readonly HttpClient _httpClient;
    private readonly PagamentoSettings _settings;
    private readonly IInscricaoRepository _inscricaoRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly ILogger<MercadoPagoService> _logger;

    private const string BaseUrl = "https://api.mercadopago.com";

    public string GatewayAtivo => "MercadoPago";

    public MercadoPagoService(
        HttpClient httpClient,
        IOptions<PagamentoSettings> settings,
        IInscricaoRepository inscricaoRepository,
        IPagamentoRepository pagamentoRepository,
        ILogger<MercadoPagoService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _inscricaoRepository = inscricaoRepository;
        _pagamentoRepository = pagamentoRepository;
        _logger = logger;

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.MercadoPago.AccessToken);
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

        // Verificar se já existe cobrança pendente
        var cobrancaExistente = await _pagamentoRepository.GetPendenteByInscricaoAsync(idInscricao);
        if (cobrancaExistente != null && cobrancaExistente.DataExpiracao > DateTime.UtcNow)
        {
            // Retornar cobrança existente
            return MapearParaResponse(cobrancaExistente, inscricao);
        }

        // Criar nova cobrança no Mercado Pago
        var dataExpiracao = DateTime.UtcNow.AddMinutes(_settings.MercadoPago.ExpiracaoMinutos);

        var request = new MercadoPagoPixRequest
        {
            TransactionAmount = inscricao.ValorFinal,
            Description = $"Inscrição {inscricao.Evento.Nome} - {inscricao.Categoria.Nome}",
            PaymentMethodId = "pix",
            DateOfExpiration = dataExpiracao.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            ExternalReference = $"INSC-{idInscricao}",
            NotificationUrl = _settings.MercadoPago.NotificationUrl,
            Payer = new MercadoPagoPayer
            {
                Email = inscricao.Piloto.Email,
                FirstName = inscricao.Piloto.Nome.Split(' ').First(),
                LastName = inscricao.Piloto.Nome.Split(' ').LastOrDefault() ?? "",
                Identification = new MercadoPagoIdentification
                {
                    Type = "CPF",
                    Number = inscricao.Piloto.Cpf
                }
            }
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Criando cobrança PIX no Mercado Pago: {Request}", json);

        var response = await _httpClient.PostAsync("/v1/payments", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro ao criar cobrança no Mercado Pago: {Status} - {Response}",
                response.StatusCode, responseContent);

            var error = JsonConvert.DeserializeObject<MercadoPagoError>(responseContent);
            throw new ExternalServiceException(
                $"Erro no Mercado Pago: {error?.Message ?? responseContent}");
        }

        var mpResponse = JsonConvert.DeserializeObject<MercadoPagoPixResponse>(responseContent);

        _logger.LogInformation("Cobrança PIX criada: ID {Id}, Status {Status}",
            mpResponse.Id, mpResponse.Status);

        // Salvar registro de pagamento
        var pagamento = new Pagamento
        {
            IdInscricao = idInscricao,
            IdExterno = mpResponse.Id.ToString(),
            Gateway = "MercadoPago",
            Valor = inscricao.ValorFinal,
            Status = MapearStatusMercadoPago(mpResponse.Status),
            QrCode = mpResponse.PointOfInteraction?.TransactionData?.QrCode,
            QrCodeBase64 = mpResponse.PointOfInteraction?.TransactionData?.QrCodeBase64,
            CopiaCola = mpResponse.PointOfInteraction?.TransactionData?.QrCode,
            DataCriacao = DateTime.UtcNow,
            DataExpiracao = dataExpiracao,
            PayloadOriginal = responseContent
        };

        await _pagamentoRepository.AddAsync(pagamento);

        // Atualizar status da inscrição
        inscricao.StatusPagamento = "AGUARDANDO_PAGAMENTO";
        inscricao.QrCodePix = pagamento.QrCode;
        inscricao.CodigoPix = pagamento.CopiaCola;
        await _inscricaoRepository.UpdateAsync(inscricao);

        return new CobrancaPixResponseDto
        {
            IdCobranca = pagamento.Id.ToString(),
            IdExterno = mpResponse.Id.ToString(),
            Valor = inscricao.ValorFinal,
            Status = pagamento.Status,
            QrCode = pagamento.QrCode,
            QrCodeBase64 = pagamento.QrCodeBase64,
            QrCodeUrl = mpResponse.PointOfInteraction?.TransactionData?.TicketUrl,
            CopiaCola = pagamento.CopiaCola,
            DataCriacao = pagamento.DataCriacao,
            DataExpiracao = pagamento.DataExpiracao,
            Gateway = "MercadoPago",
            IdInscricao = idInscricao,
            NomePiloto = inscricao.Piloto.Nome,
            NomeEvento = inscricao.Evento.Nome,
            Descricao = $"Inscrição {inscricao.Categoria.Nome} - Moto #{inscricao.NumeroMoto}"
        };
    }

    public async Task<CobrancaPixResponseDto> CriarCobrancaPixMultiplasAsync(List<int> idsInscricoes)
    {
        if (idsInscricoes == null || !idsInscricoes.Any())
        {
            throw new ValidationException("Informe pelo menos uma inscrição");
        }

        // Buscar todas as inscrições
        var inscricoes = new List<Inscricao>();
        decimal valorTotal = 0;
        int? idPiloto = null;
        int? idEvento = null;

        foreach (var id in idsInscricoes)
        {
            var inscricao = await _inscricaoRepository.GetByIdWithDetailsAsync(id);

            if (inscricao == null)
            {
                throw new NotFoundException($"Inscrição {id} não encontrada");
            }

            if (inscricao.StatusPagamento == "PAGO")
            {
                throw new ValidationException($"Inscrição {id} já está paga");
            }

            // Validar mesmo piloto e evento
            if (idPiloto == null)
            {
                idPiloto = inscricao.IdPiloto;
                idEvento = inscricao.IdEvento;
            }
            else if (inscricao.IdPiloto != idPiloto || inscricao.IdEvento != idEvento)
            {
                throw new ValidationException("Todas as inscrições devem ser do mesmo piloto e evento");
            }

            inscricoes.Add(inscricao);
            valorTotal += inscricao.ValorFinal;
        }

        var primeiraInscricao = inscricoes.First();
        var categorias = string.Join(", ", inscricoes.Select(i => i.Categoria.Nome));

        // Criar cobrança única
        var dataExpiracao = DateTime.UtcNow.AddMinutes(_settings.MercadoPago.ExpiracaoMinutos);

        var request = new MercadoPagoPixRequest
        {
            TransactionAmount = valorTotal,
            Description = $"Inscrições {primeiraInscricao.Evento.Nome} - {categorias}",
            PaymentMethodId = "pix",
            DateOfExpiration = dataExpiracao.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            ExternalReference = $"MULTI-{string.Join("-", idsInscricoes)}",
            NotificationUrl = _settings.MercadoPago.NotificationUrl,
            Payer = new MercadoPagoPayer
            {
                Email = primeiraInscricao.Piloto.Email,
                FirstName = primeiraInscricao.Piloto.Nome.Split(' ').First(),
                LastName = primeiraInscricao.Piloto.Nome.Split(' ').LastOrDefault() ?? "",
                Identification = new MercadoPagoIdentification
                {
                    Type = "CPF",
                    Number = primeiraInscricao.Piloto.Cpf
                }
            }
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/payments", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonConvert.DeserializeObject<MercadoPagoError>(responseContent);
            throw new ExternalServiceException(
                $"Erro no Mercado Pago: {error?.Message ?? responseContent}");
        }

        var mpResponse = JsonConvert.DeserializeObject<MercadoPagoPixResponse>(responseContent);

        // Criar registro de pagamento para cada inscrição
        foreach (var inscricao in inscricoes)
        {
            var pagamento = new Pagamento
            {
                IdInscricao = inscricao.Id,
                IdExterno = mpResponse.Id.ToString(),
                Gateway = "MercadoPago",
                Valor = inscricao.ValorFinal,
                Status = MapearStatusMercadoPago(mpResponse.Status),
                QrCode = mpResponse.PointOfInteraction?.TransactionData?.QrCode,
                QrCodeBase64 = mpResponse.PointOfInteraction?.TransactionData?.QrCodeBase64,
                CopiaCola = mpResponse.PointOfInteraction?.TransactionData?.QrCode,
                DataCriacao = DateTime.UtcNow,
                DataExpiracao = dataExpiracao,
                PayloadOriginal = responseContent,
                Observacoes = $"Cobrança múltipla - Total: R$ {valorTotal:N2}"
            };

            await _pagamentoRepository.AddAsync(pagamento);

            inscricao.StatusPagamento = "AGUARDANDO_PAGAMENTO";
            inscricao.QrCodePix = pagamento.QrCode;
            inscricao.CodigoPix = pagamento.CopiaCola;
            await _inscricaoRepository.UpdateAsync(inscricao);
        }

        return new CobrancaPixResponseDto
        {
            IdCobranca = mpResponse.Id.ToString(),
            IdExterno = mpResponse.Id.ToString(),
            Valor = valorTotal,
            Status = MapearStatusMercadoPago(mpResponse.Status),
            QrCode = mpResponse.PointOfInteraction?.TransactionData?.QrCode,
            QrCodeBase64 = mpResponse.PointOfInteraction?.TransactionData?.QrCodeBase64,
            QrCodeUrl = mpResponse.PointOfInteraction?.TransactionData?.TicketUrl,
            CopiaCola = mpResponse.PointOfInteraction?.TransactionData?.QrCode,
            DataCriacao = DateTime.UtcNow,
            DataExpiracao = dataExpiracao,
            Gateway = "MercadoPago",
            IdInscricao = primeiraInscricao.Id,
            NomePiloto = primeiraInscricao.Piloto.Nome,
            NomeEvento = primeiraInscricao.Evento.Nome,
            Descricao = $"Inscrições: {categorias}"
        };
    }

    public async Task<ConsultaPagamentoResponseDto> ConsultarPagamentoAsync(string idExterno)
    {
        _logger.LogInformation("Consultando pagamento no Mercado Pago: {Id}", idExterno);

        var response = await _httpClient.GetAsync($"/v1/payments/{idExterno}");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro ao consultar pagamento: {Status} - {Response}",
                response.StatusCode, responseContent);
            throw new ExternalServiceException("Erro ao consultar pagamento no Mercado Pago");
        }

        var mpResponse = JsonConvert.DeserializeObject<MercadoPagoPixResponse>(responseContent);

        return new ConsultaPagamentoResponseDto
        {
            IdCobranca = mpResponse.Id.ToString(),
            IdExterno = mpResponse.Id.ToString(),
            Valor = mpResponse.TransactionAmount,
            ValorPago = mpResponse.Status == "approved" ? mpResponse.TransactionAmount : null,
            Status = MapearStatusMercadoPago(mpResponse.Status),
            DataPagamento = mpResponse.DateApproved,
            TransacaoId = mpResponse.Id.ToString(),
            PagadorNome = $"{mpResponse.Payer?.FirstName} {mpResponse.Payer?.LastName}".Trim(),
            PagadorDocumento = mpResponse.Payer?.Identification?.Number
        };
    }

    public async Task<ConsultaPagamentoResponseDto> ConsultarPagamentoPorInscricaoAsync(int idInscricao)
    {
        var pagamento = await _pagamentoRepository.GetUltimoByInscricaoAsync(idInscricao);

        if (pagamento == null)
        {
            throw new NotFoundException("Nenhum pagamento encontrado para esta inscrição");
        }

        return await ConsultarPagamentoAsync(pagamento.IdExterno);
    }

    public async Task<bool> ProcessarWebhookAsync(WebhookPagamentoDto webhook)
    {
        _logger.LogInformation("Processando webhook Mercado Pago: {IdExterno}, Status: {Status}",
            webhook.IdExterno, webhook.Status);

        // Buscar pagamentos com este ID externo
        var pagamentos = await _pagamentoRepository.GetByIdExternoAsync(webhook.IdExterno);

        if (!pagamentos.Any())
        {
            _logger.LogWarning("Pagamento não encontrado para ID externo: {IdExterno}", webhook.IdExterno);
            return false;
        }

        var novoStatus = MapearStatusMercadoPago(webhook.Status);

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

            // Atualizar inscrição
            var inscricao = await _inscricaoRepository.GetByIdAsync(pagamento.IdInscricao);
            if (inscricao != null)
            {
                if (novoStatus == StatusPagamentoPix.Pago)
                {
                    inscricao.StatusPagamento = "PAGO";
                    inscricao.MetodoPagamento = "PIX";
                    inscricao.TransacaoId = webhook.TransacaoId;
                    inscricao.DataPagamento = webhook.DataPagamento ?? DateTime.UtcNow;

                    _logger.LogInformation(
                        "Pagamento confirmado: Inscrição {Id}, Piloto: {Piloto}",
                        inscricao.Id, inscricao.IdPiloto);
                }
                else if (novoStatus == StatusPagamentoPix.Expirado ||
                         novoStatus == StatusPagamentoPix.Cancelado)
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
        _logger.LogInformation("Cancelando cobrança no Mercado Pago: {Id}", idExterno);

        var content = new StringContent(
            JsonConvert.SerializeObject(new { status = "cancelled" }),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PutAsync($"/v1/payments/{idExterno}", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erro ao cancelar cobrança: {Status}", response.StatusCode);
            return false;
        }

        // Atualizar registros locais
        var pagamentos = await _pagamentoRepository.GetByIdExternoAsync(idExterno);
        foreach (var pagamento in pagamentos)
        {
            pagamento.Status = StatusPagamentoPix.Cancelado;
            pagamento.DataAtualizacao = DateTime.UtcNow;
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
                // Consultar status atual no MP
                var status = await ConsultarPagamentoAsync(pagamento.IdExterno);

                if (status.Status == StatusPagamentoPix.Pago)
                {
                    // Foi pago! Atualizar
                    await ProcessarWebhookAsync(new WebhookPagamentoDto
                    {
                        IdExterno = pagamento.IdExterno,
                        Status = "approved",
                        DataPagamento = status.DataPagamento,
                        TransacaoId = status.TransacaoId,
                        Gateway = "MercadoPago"
                    });
                }
                else
                {
                    // Marcar como expirada
                    pagamento.Status = StatusPagamentoPix.Expirado;
                    pagamento.DataAtualizacao = DateTime.UtcNow;
                    await _pagamentoRepository.UpdateAsync(pagamento);

                    // Atualizar inscrição
                    var inscricao = await _inscricaoRepository.GetByIdAsync(pagamento.IdInscricao);
                    if (inscricao != null && inscricao.StatusPagamento == "AGUARDANDO_PAGAMENTO")
                    {
                        inscricao.StatusPagamento = "PENDENTE";
                        await _inscricaoRepository.UpdateAsync(inscricao);
                    }

                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar cobrança expirada: {Id}", pagamento.Id);
            }
        }

        _logger.LogInformation("Verificação de cobranças expiradas: {Count} atualizadas", count);

        return count;
    }

    #region Métodos Privados

    private string MapearStatusMercadoPago(string statusMp)
    {
        return statusMp?.ToLower() switch
        {
            "pending" => StatusPagamentoPix.Pendente,
            "approved" => StatusPagamentoPix.Pago,
            "authorized" => StatusPagamentoPix.Processando,
            "in_process" => StatusPagamentoPix.Processando,
            "in_mediation" => StatusPagamentoPix.Processando,
            "rejected" => StatusPagamentoPix.Cancelado,
            "cancelled" => StatusPagamentoPix.Cancelado,
            "refunded" => StatusPagamentoPix.Reembolsado,
            "charged_back" => StatusPagamentoPix.Reembolsado,
            _ => StatusPagamentoPix.Pendente
        };
    }

    private CobrancaPixResponseDto MapearParaResponse(Pagamento pagamento, Inscricao inscricao)
    {
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
            Gateway = pagamento.Gateway,
            IdInscricao = inscricao.Id,
            NomePiloto = inscricao.Piloto?.Nome ?? "",
            NomeEvento = inscricao.Evento?.Nome ?? "",
            Descricao = $"Inscrição {inscricao.Categoria?.Nome} - Moto #{inscricao.NumeroMoto}"
        };
    }

    #endregion
}
