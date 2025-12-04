namespace DevNationCrono.API.Models.DTOs;

public class AuthResultDto
{
    public bool Sucesso { get; set; }
    public string? Token { get; set; }
    public string? Mensagem { get; set; }
    public DateTime? ExpiraEm { get; set; }
    public string? DeviceId { get; set; }
    public string? Nome { get; set; }
}