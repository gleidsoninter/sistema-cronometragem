using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class LoteLeituraDto
{
    [Required(ErrorMessage = "DeviceId é obrigatório")]
    public string DeviceId { get; set; }

    [Required(ErrorMessage = "IdEtapa é obrigatório")]
    public int IdEtapa { get; set; }

    [Required(ErrorMessage = "Leituras são obrigatórias")]
    [MinLength(1, ErrorMessage = "Deve haver pelo menos uma leitura")]
    public List<LeituraItemDto> Leituras { get; set; }
}
