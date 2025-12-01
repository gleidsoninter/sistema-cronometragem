using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNationCrono.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;
    private readonly ILogger<CategoriasController> _logger;

    public CategoriasController(
        ICategoriaService categoriaService,
        ILogger<CategoriasController> logger)
    {
        _categoriaService = categoriaService;
        _logger = logger;
    }

    /// <summary>
    /// Lista categorias de um evento
    /// </summary>
    [HttpGet("evento/{idEvento}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoriaDto>), 200)]
    public async Task<ActionResult<List<CategoriaDto>>> GetByEvento(int idEvento)
    {
        var categorias = await _categoriaService.GetByEventoAsync(idEvento);
        return Ok(categorias);
    }

    /// <summary>
    /// Lista categorias ativas de um evento (para inscrição)
    /// </summary>
    [HttpGet("evento/{idEvento}/ativas")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoriaResumoDto>), 200)]
    public async Task<ActionResult<List<CategoriaResumoDto>>> GetActivesByEvento(int idEvento)
    {
        var categorias = await _categoriaService.GetActivesByEventoAsync(idEvento);
        return Ok(categorias);
    }

    /// <summary>
    /// Busca categoria por ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoriaDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CategoriaDto>> GetById(int id)
    {
        var categoria = await _categoriaService.GetByIdAsync(id);

        if (categoria == null)
            return NotFound($"Categoria com ID {id} não encontrada");

        return Ok(categoria);
    }

    /// <summary>
    /// Cria nova categoria
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(CategoriaDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CategoriaDto>> Create([FromBody] CategoriaCreateDto dto)
    {
        var categoria = await _categoriaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
    }

    /// <summary>
    /// Atualiza categoria
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(typeof(CategoriaDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CategoriaDto>> Update(int id, [FromBody] CategoriaUpdateDto dto)
    {
        var categoria = await _categoriaService.UpdateAsync(id, dto);
        return Ok(categoria);
    }

    /// <summary>
    /// Deleta categoria (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Organizador")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoriaService.DeleteAsync(id);
        return NoContent();
    }
}
