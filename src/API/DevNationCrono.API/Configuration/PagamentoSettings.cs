namespace DevNationCrono.API.Configuration;

public class PagamentoSettings
{
    public string GatewayAtivo { get; set; } = "MercadoPago";
    public MercadoPagoSettings MercadoPago { get; set; }
    public AsaasSettings Asaas { get; set; }
    public PixConfig PixConfig { get; set; }
}

public class MercadoPagoSettings
{
    public string AccessToken { get; set; }
    public string PublicKey { get; set; }
    public string WebhookSecret { get; set; }
    public string NotificationUrl { get; set; }
    public int ExpiracaoMinutos { get; set; } = 30;
    public bool Sandbox { get; set; } = true;
}

public class AsaasSettings
{
    public string ApiKey { get; set; }
    public string WebhookToken { get; set; }
    public string NotificationUrl { get; set; }
    public int ExpiracaoMinutos { get; set; } = 30;
    public bool Sandbox { get; set; } = true;
    public string BaseUrl { get; set; } = "https://sandbox.asaas.com/api/v3";
}

public class PixConfig
{
    public string ChavePix { get; set; }
    public string TipoChave { get; set; } // CPF, CNPJ, EMAIL, TELEFONE, ALEATORIA
    public string NomeBeneficiario { get; set; }
    public string CidadeBeneficiario { get; set; }
}
