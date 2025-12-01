using DevNationCrono.API.Data;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class DispositivoColetorRepository : IDispositivoColetorRepository
{
    private readonly ApplicationDbContext _context;

    public DispositivoColetorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DispositivoColetor?> GetByIdAsync(int id)
    {
        return await _context.DispositivosColetores
            .Include(d => d.Evento)
            .Include(d => d.Etapa)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
    public async Task<DispositivoColetor?> GetByDeviceIdAsync(string deviceId)
    {
        return await _context.DispositivosColetores
            .Include(d => d.Evento)
            .Include(d => d.Etapa)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
    }

    public async Task<List<DispositivoColetor>> GetByEventoAsync(int idEvento)
    {
        return await _context.DispositivosColetores
            .Where(d => d.IdEvento == idEvento && d.Ativo)
            .OrderBy(d => d.Nome)
            .ToListAsync();
    }

    public async Task<DispositivoColetor> AddAsync(DispositivoColetor coletor)
    {
        await _context.DispositivosColetores.AddAsync(coletor);
        await _context.SaveChangesAsync();
        return coletor;
    }

    public async Task UpdateAsync(DispositivoColetor coletor)
    {
        coletor.DataAtualizacao = DateTime.UtcNow;
        _context.DispositivosColetores.Update(coletor);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeviceIdExistsAsync(string deviceId)
    {
        return await _context.DispositivosColetores
            .AnyAsync(d => d.DeviceId == deviceId);
    }
}