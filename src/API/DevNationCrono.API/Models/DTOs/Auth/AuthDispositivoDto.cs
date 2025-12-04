using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class AuthDispositivoDto
{
    [Required(ErrorMessage = "DeviceId é obrigatório")]
    public string DeviceId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do dispositivo: E (Entrada), S (Saída), P (Passagem)
    /// </summary>
    [Required(ErrorMessage = "Tipo é obrigatório")]
    public string Tipo { get; set; } = "P";

    /// <summary>
    /// ID da etapa que o dispositivo vai coletar
    /// </summary>
    public int? IdEtapa { get; set; }
}