namespace DevNationCrono.API.Models.DTOs;

public class AtualizarDispositivoDto
{
    public string? Nome { get; set; }
    public string? TipoPonto { get; set; }
    public int? IdEtapa { get; set; }
    public int? NumeroEspecial { get; set; }
    public bool? Ativo { get; set; }
    public string? NovaSenha { get; set; }
}