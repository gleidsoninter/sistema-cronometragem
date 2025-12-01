namespace DevNationCrono.API.Configuration;

public class JwtSettings
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationHours { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}
