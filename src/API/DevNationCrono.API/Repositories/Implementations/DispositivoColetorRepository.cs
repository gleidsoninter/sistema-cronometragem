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
                .ThenInclude(e => e.Modalidade)
            .Include(d => d.Etapa)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.Ativo);
    }

    public async Task<DispositivoColetor?> GetByDeviceIdEtapaAsync(string deviceId, int idEtapa)
    {
        return await _context.DispositivosColetores
            .Include(d => d.Evento)
                .ThenInclude(e => e.Modalidade)
            .Include(d => d.Etapa)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId
                                   && d.IdEtapa == idEtapa
                                   && d.Ativo);
    }

    public async Task<List<DispositivoColetor>> GetByEtapaAsync(int idEtapa)
    {
        return await _context.DispositivosColetores
            .Include(d => d.Evento)
            .Include(d => d.Etapa)
            .Where(d => d.IdEtapa == idEtapa && d.Ativo)
            .OrderBy(d => d.IdEspecial)
            .ThenBy(d => d.Tipo)
            .ToListAsync();
    }

    public async Task<List<DispositivoColetor>> GetByEventoAsync(int idEvento)
    {
        return await _context.DispositivosColetores
            .Include(d => d.Etapa)
            .Where(d => d.IdEvento == idEvento && d.Ativo)
            .OrderBy(d => d.IdEtapa)
            .ThenBy(d => d.IdEspecial)
            .ToListAsync();
    }

    public async Task<DispositivoColetor> AddAsync(DispositivoColetor dispositivo)
    {
        await _context.DispositivosColetores.AddAsync(dispositivo);
        await _context.SaveChangesAsync();
        return dispositivo;
    }

    public async Task UpdateAsync(DispositivoColetor dispositivo)
    {
        _context.DispositivosColetores.Update(dispositivo);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteDeviceIdAsync(string deviceId, int? excludeId = null)
    {
        var query = _context.DispositivosColetores
            .Where(d => d.DeviceId == deviceId && d.Ativo);

        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task AtualizarStatusConexaoAsync(int id, string status)
    {
        var dispositivo = await _context.DispositivosColetores.FindAsync(id);
        if (dispositivo != null)
        {
            dispositivo.StatusConexao = status;
            dispositivo.UltimaConexao = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementarLeiturasAsync(int id)
    {
        var dispositivo = await _context.DispositivosColetores.FindAsync(id);
        if (dispositivo != null)
        {
            dispositivo.TotalLeituras++;
            dispositivo.UltimaLeitura = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}