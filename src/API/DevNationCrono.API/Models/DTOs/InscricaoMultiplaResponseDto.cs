namespace DevNationCrono.API.Models.DTOs;

public class InscricaoMultiplaResponseDto
{
    public int IdPiloto { get; set; }
    public string NomePiloto { get; set; }
    public int IdEvento { get; set; }
    public string NomeEvento { get; set; }
    public int NumeroMoto { get; set; }
    public List<InscricaoCategoriaDto> Inscricoes { get; set; }
    public decimal ValorTotalOriginal { get; set; }
    public decimal ValorTotalDesconto { get; set; }
    public decimal ValorTotalFinal { get; set; }
    public string QrCodePix { get; set; }
    public string CodigoPix { get; set; }
}
