using Newtonsoft.Json;

namespace DevNationCrono.API.Models.Pagamento.Asaas;

public class AsaasClienteRequest
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("cpfCnpj")]
    public string CpfCnpj { get; set; }

    [JsonProperty("mobilePhone")]
    public string MobilePhone { get; set; }

    [JsonProperty("externalReference")]
    public string ExternalReference { get; set; }
}

public class AsaasClienteResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("cpfCnpj")]
    public string CpfCnpj { get; set; }
}

// ===== CRIAR COBRANÇA PIX =====
public class AsaasCobrancaRequest
{
    [JsonProperty("customer")]
    public string Customer { get; set; } // ID do cliente no Asaas

    [JsonProperty("billingType")]
    public string BillingType { get; set; } = "PIX";

    [JsonProperty("value")]
    public decimal Value { get; set; }

    [JsonProperty("dueDate")]
    public string DueDate { get; set; } // YYYY-MM-DD

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("externalReference")]
    public string ExternalReference { get; set; }

    [JsonProperty("postalService")]
    public bool PostalService { get; set; } = false;
}

public class AsaasCobrancaResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("customer")]
    public string Customer { get; set; }

    [JsonProperty("value")]
    public decimal Value { get; set; }

    [JsonProperty("netValue")]
    public decimal NetValue { get; set; }

    [JsonProperty("billingType")]
    public string BillingType { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("dueDate")]
    public string DueDate { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("externalReference")]
    public string ExternalReference { get; set; }

    [JsonProperty("invoiceUrl")]
    public string InvoiceUrl { get; set; }

    [JsonProperty("bankSlipUrl")]
    public string BankSlipUrl { get; set; }
}

// ===== QR CODE PIX =====
public class AsaasPixQrCodeResponse
{
    [JsonProperty("encodedImage")]
    public string EncodedImage { get; set; } // Base64

    [JsonProperty("payload")]
    public string Payload { get; set; } // Copia e cola

    [JsonProperty("expirationDate")]
    public DateTime ExpirationDate { get; set; }
}

// ===== WEBHOOK =====
public class AsaasWebhook
{
    [JsonProperty("event")]
    public string Event { get; set; }

    [JsonProperty("payment")]
    public AsaasWebhookPayment Payment { get; set; }
}

public class AsaasWebhookPayment
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("customer")]
    public string Customer { get; set; }

    [JsonProperty("value")]
    public decimal Value { get; set; }

    [JsonProperty("netValue")]
    public decimal NetValue { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("billingType")]
    public string BillingType { get; set; }

    [JsonProperty("confirmedDate")]
    public string ConfirmedDate { get; set; }

    [JsonProperty("paymentDate")]
    public string PaymentDate { get; set; }

    [JsonProperty("externalReference")]
    public string ExternalReference { get; set; }

    [JsonProperty("transactionReceiptUrl")]
    public string TransactionReceiptUrl { get; set; }
}

// ===== ERRO =====
public class AsaasError
{
    [JsonProperty("errors")]
    public List<AsaasErrorDetail> Errors { get; set; }
}

public class AsaasErrorDetail
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}
