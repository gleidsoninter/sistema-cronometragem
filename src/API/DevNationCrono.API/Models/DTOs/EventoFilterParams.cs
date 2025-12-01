using DevNationCrono.API.Models.Pagination;

namespace DevNationCrono.API.Models.DTOs;

public class EventoFilterParams : PaginationParams
{
    public string? Nome { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public int? IdModalidade { get; set; }
    public string? Status { get; set; }
    public bool? InscricoesAbertas { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public bool? Ativo { get; set; }
}
