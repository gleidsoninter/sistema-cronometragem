namespace DevNationCrono.API.Models.DTOs;

public class LoteLeituraResponseDto
{
    public int TotalRecebidas { get; set; }
    public int TotalProcessadas { get; set; }
    public int TotalDuplicadas { get; set; }
    public int TotalErros { get; set; }
    public List<LeituraResponseDto> Leituras { get; set; }
    public List<string> Erros { get; set; }
}
