using DevNationCrono.API.Data;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class EtapaRepository : IEtapaRepository
{
    private readonly ApplicationDbContext _context;

    public EtapaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Etapa?> GetByIdAsync(int id)
    {
        return await _context.Etapas
            .Include(e => e.Evento)
                .ThenInclude(ev => ev.Modalidade)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Etapa>> GetByEventoAsync(int idEvento)
    {
        return await _context.Etapas
            .Include(e => e.Evento)
                .ThenInclude(ev => ev.Modalidade)
            .Where(e => e.IdEvento == idEvento)
            .OrderBy(e => e.NumeroEtapa)
            .ToListAsync();
    }

    public async Task<Etapa?> GetByEventoNumeroAsync(int idEvento, int numeroEtapa)
    {
        return await _context.Etapas
            .FirstOrDefaultAsync(e => e.IdEvento == idEvento && e.NumeroEtapa == numeroEtapa);
    }

    public async Task<Etapa> AddAsync(Etapa etapa)
    {
        await _context.Etapas.AddAsync(etapa);
        await _context.SaveChangesAsync();
        return etapa;
    }

    public async Task UpdateAsync(Etapa etapa)
    {
        etapa.DataAtualizacao = DateTime.UtcNow;
        _context.Etapas.Update(etapa);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var etapa = await GetByIdAsync(id);
        if (etapa != null)
        {
            etapa.Status = "CANCELADA";
            await UpdateAsync(etapa);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Etapas.AnyAsync(e => e.Id == id);
    }

    public async Task<bool> NumeroExistsNoEventoAsync(int numeroEtapa, int idEvento, int? excludeId = null)
    {
        var query = _context.Etapas
            .Where(e => e.NumeroEtapa == numeroEtapa && e.IdEvento == idEvento);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> CountLeiturasAsync(int id)
    {
        return await _context.Tempos.CountAsync(t => t.IdEtapa == id);
    }

    public async Task<int> CountColetoresAsync(int id)
    {
        return await _context.DispositivosColetores
            .CountAsync(d => d.IdEtapa == id && d.Ativo);
    }

    public async Task<int> GetProximoNumeroEtapaAsync(int idEvento)
    {
        var max = await _context.Etapas
            .Where(e => e.IdEvento == idEvento)
            .MaxAsync(e => (int?)e.NumeroEtapa) ?? 0;

        return max + 1;
    }
}
