using DevNationCrono.API.Data;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly ApplicationDbContext _context;

    public CategoriaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Categoria?> GetByIdAsync(int id)
    {
        return await _context.Categorias
            .Include(c => c.Modalidade)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Categoria>> GetByModalidadeAsync(int idModalidade)
    {
        return await _context.Categorias
            .Include(c => c.Modalidade)
            .Where(c => c.IdModalidade == idModalidade)
            .OrderBy(c => c.Ordem)
            .ThenBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<List<Categoria>> GetByEtapaAsync(int idEtapa)
    {
        return await _context.Set<EtapaCategoria>()
            .Include(ec => ec.Categoria)
            .Where(ec => ec.IdEtapa == idEtapa)
            .OrderBy(ec => ec.OrdemLargada)
            .ThenBy(ec => ec.Categoria.Nome)
            .Select(ec => ec.Categoria)
            .ToListAsync();
    }

    public async Task<List<Categoria>> GetActivesByModalidadeAsync(int idModalidade)
    {
        return await _context.Categorias
            .Include(c => c.Modalidade)
            .Where(c => c.IdModalidade == idModalidade && c.Ativo)
            .OrderBy(c => c.Ordem)
            .ThenBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<Categoria> AddAsync(Categoria categoria)
    {
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        return categoria;
    }

    public async Task UpdateAsync(Categoria categoria)
    {
        _context.Categorias.Update(categoria);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var categoria = await GetByIdAsync(id);
        if (categoria != null)
        {
            categoria.Ativo = false;
            await UpdateAsync(categoria);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Categorias.AnyAsync(c => c.Id == id);
    }

    public async Task<bool> NomeExistsNaModalidadeAsync(string nome, int idModalidade, int? excludeId = null)
    {
        var query = _context.Categorias
            .Where(c => c.Nome.ToLower() == nome.ToLower() && c.IdModalidade == idModalidade);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> CountInscritosAsync(int id)
    {
        return await _context.Inscricoes
            .CountAsync(i => i.IdCategoria == id && i.StatusPagamento != "CANCELADO");
    }
}
