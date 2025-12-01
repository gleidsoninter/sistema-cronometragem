using DevNationCrono.API.Data;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class ModalidadeRepository : IModalidadeRepository
{
    private readonly ApplicationDbContext _context;

    public ModalidadeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Modalidade?> GetByIdAsync(int id)
    {
        return await _context.Modalidades
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Modalidade?> GetByIdWithEventosAsync(int id)
    {
        return await _context.Modalidades
            .Include(m => m.Eventos.Where(e => e.Ativo))
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Modalidade?> GetByNomeAsync(string nome)
    {
        return await _context.Modalidades
            .FirstOrDefaultAsync(m => m.Nome.ToLower() == nome.ToLower());
    }

    public async Task<List<Modalidade>> GetAllAsync()
    {
        return await _context.Modalidades
            .OrderBy(m => m.Nome)
            .ToListAsync();
    }

    public async Task<List<Modalidade>> GetActivesAsync()
    {
        return await _context.Modalidades
            .Where(m => m.Ativo)
            .OrderBy(m => m.Nome)
            .ToListAsync();
    }

    public async Task<List<Modalidade>> GetByTipoAsync(string tipoCronometragem)
    {
        return await _context.Modalidades
            .Where(m => m.TipoCronometragem == tipoCronometragem && m.Ativo)
            .OrderBy(m => m.Nome)
            .ToListAsync();
    }

    public async Task<Modalidade> AddAsync(Modalidade modalidade)
    {
        await _context.Modalidades.AddAsync(modalidade);
        await _context.SaveChangesAsync();
        return modalidade;
    }

    public async Task UpdateAsync(Modalidade modalidade)
    {
        _context.Modalidades.Update(modalidade);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var modalidade = await GetByIdAsync(id);
        if (modalidade != null)
        {
            modalidade.Ativo = false;
            await UpdateAsync(modalidade);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Modalidades.AnyAsync(m => m.Id == id);
    }

    public async Task<bool> NomeExistsAsync(string nome, int? excludeId = null)
    {
        var query = _context.Modalidades
            .Where(m => m.Nome.ToLower() == nome.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> CountEventosAsync(int id)
    {
        return await _context.Eventos
            .CountAsync(e => e.IdModalidade == id && e.Ativo);
    }
}
