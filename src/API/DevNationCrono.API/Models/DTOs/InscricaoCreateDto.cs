using System.ComponentModel.DataAnnotations;

namespace DevNationCrono.API.Models.DTOs;

public class InscricaoCreateDto
{
    [Required(ErrorMessage = "Piloto é obrigatório")]
    public int IdPiloto { get; set; }

    [Required(ErrorMessage = "Evento é obrigatório")]
    public int IdEvento { get; set; }

    [Required(ErrorMessage = "Categoria é obrigatória")]
    public int IdCategoria { get; set; }

    [Required(ErrorMessage = "Etapa é obrigatória")]
    public int IdEtapa { get; set; }

    /// <summary>
    /// Opcional. Se não informado, será gerado automaticamente
    /// </summary>
    public int? NumeroMoto { get; set; }

    [StringLength(500)]
    public string? Observacoes { get; set; }
}
