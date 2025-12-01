using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Models.DTOs;

public class LeituraFilterParams : PaginationParams
{
    public int? IdEtapa { get; set; }
    public int? NumeroMoto { get; set; }
    public string? Tipo { get; set; }
    public int? IdEspecial { get; set; }
    public int? Volta { get; set; }
    public int? IdDispositivo { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public bool? Descartada { get; set; }
    public bool? Sincronizado { get; set; }
}
