namespace DevNationCrono.API.Models.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; }
    public string Senha { get; set; }
}

public class LoginResponseDto
{
    public bool Sucesso { get; set; }
    public string? Token { get; set; }
    public string? Mensagem { get; set; }
    public UsuarioDto? Usuario { get; set; }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

public class RegisterColetorDto
{
    public int IdEvento { get; set; }
    public int IdEtapa { get; set; }
    public string Nome { get; set; }
    public string Tipo { get; set; } // "ENTRADA", "SAIDA", "PASSAGEM"
    public int? IdEspecial { get; set; }
    public string DeviceId { get; set; }
    public string? Modelo { get; set; }
    public string? VersaoApp { get; set; }
}

public class ColetorLoginDto
{
    public string DeviceId { get; set; }
    public string Token { get; set; } // Token temporário de registro
}
