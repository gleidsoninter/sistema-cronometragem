namespace DevNationCrono.API.Models.DTOs;

public class LeituraResponseDto
{
    public long Id { get; set; }
    public int NumeroMoto { get; set; }
    public string NomePiloto { get; set; }
    public string Categoria { get; set; }
    public DateTime Timestamp { get; set; }
    public string Tipo { get; set; }
    public string TipoDescricao { get; set; } // "Entrada", "Saída", "Passagem"
    public int? IdEspecial { get; set; }
    public int Volta { get; set; }
    public decimal? TempoCalculadoSegundos { get; set; }
    public string TempoFormatado { get; set; }
    public bool MelhorVolta { get; set; }
    public string Status { get; set; } // OK, DUPLICADA, ERRO, SEM_INSCRICAO
    public string Mensagem { get; set; }
    public bool Sincronizado { get; set; }
}
