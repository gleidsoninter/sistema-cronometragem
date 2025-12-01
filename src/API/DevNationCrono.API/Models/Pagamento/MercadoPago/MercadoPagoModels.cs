using Newtonsoft.Json;

namespace DevNationCrono.API.Models.Pagamento.MercadoPago;

public class MercadoPagoPixRequest
{
    [JsonProperty("transaction_amount")]
    public decimal TransactionAmount { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("payment_method_id")]
    public string PaymentMethodId { get; set; } = "pix";

    [JsonProperty("payer")]
    public MercadoPagoPayer Payer { get; set; }

    [JsonProperty("date_of_expiration")]
    public string DateOfExpiration { get; set; }

    [JsonProperty("external_reference")]
    public string ExternalReference { get; set; }

    [JsonProperty("notification_url")]
    public string NotificationUrl { get; set; }
}

public class MercadoPagoPayer
{
    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("identification")]
    public MercadoPagoIdentification Identification { get; set; }
}

public class MercadoPagoIdentification
{
    [JsonProperty("type")]
    public string Type { get; set; } = "CPF";

    [JsonProperty("number")]
    public string Number { get; set; }
}

// ===== RESPONSE DO MERCADO PAGO =====
public class MercadoPagoPixResponse
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("status_detail")]
    public string StatusDetail { get; set; }

    [JsonProperty("transaction_amount")]
    public decimal TransactionAmount { get; set; }

    [JsonProperty("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("date_of_expiration")]
    public DateTime? DateOfExpiration { get; set; }

    [JsonProperty("date_approved")]
    public DateTime? DateApproved { get; set; }

    [JsonProperty("external_reference")]
    public string ExternalReference { get; set; }

    [JsonProperty("point_of_interaction")]
    public PointOfInteraction PointOfInteraction { get; set; }

    [JsonProperty("payer")]
    public MercadoPagoPayerResponse Payer { get; set; }
}

public class PointOfInteraction
{
    [JsonProperty("transaction_data")]
    public TransactionData TransactionData { get; set; }
}

public class TransactionData
{
    [JsonProperty("qr_code")]
    public string QrCode { get; set; }

    [JsonProperty("qr_code_base64")]
    public string QrCodeBase64 { get; set; }

    [JsonProperty("ticket_url")]
    public string TicketUrl { get; set; }
}

public class MercadoPagoPayerResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("identification")]
    public MercadoPagoIdentification Identification { get; set; }
}

// ===== WEBHOOK DO MERCADO PAGO =====
public class MercadoPagoWebhook
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("live_mode")]
    public bool LiveMode { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("api_version")]
    public string ApiVersion { get; set; }

    [JsonProperty("action")]
    public string Action { get; set; }

    [JsonProperty("data")]
    public MercadoPagoWebhookData Data { get; set; }
}

public class MercadoPagoWebhookData
{
    [JsonProperty("id")]
    public string Id { get; set; }
}

// ===== ERRO DO MERCADO PAGO =====
public class MercadoPagoError
{
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("cause")]
    public List<MercadoPagoCause> Cause { get; set; }
}

public class MercadoPagoCause
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}
