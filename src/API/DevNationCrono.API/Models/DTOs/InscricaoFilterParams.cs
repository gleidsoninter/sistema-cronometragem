using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Models.DTOs;

public class InscricaoFilterParams : PaginationParams
{
    public int? IdEvento { get; set; }
    public int? IdEtapa { get; set; }
    public int? IdCategoria { get; set; }
    public int? IdPiloto { get; set; }
    public int? NumeroMoto { get; set; }
    public string? StatusPagamento { get; set; }
    public string? NomePiloto { get; set; }
    public bool? Ativo { get; set; }
}
