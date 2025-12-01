using DevNationCrono.API.Data;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class TempoRepository : ITempoRepository
{
    private readonly ApplicationDbContext _context;

    public TempoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Buscar

    public async Task<Tempo?> GetByIdAsync(long id)
    {
        return await _context.Tempos
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Piloto)
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Categoria)
            .Include(t => t.Dispositivo)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Tempo>> GetByEtapaAsync(int idEtapa)
    {
        return await _context.Tempos
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Piloto)
            .Where(t => t.IdEtapa == idEtapa && !t.Descartada)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<List<Tempo>> GetByEtapaEVoltaAsync(int idEtapa, int volta)
    {
        return await _context.Tempos
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Piloto)
            .Where(t => t.IdEtapa == idEtapa && t.Volta == volta && !t.Descartada)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<List<Tempo>> GetByNumeroMotoEtapaAsync(int numeroMoto, int idEtapa)
    {
        return await _context.Tempos
            .Include(t => t.Inscricao)
            .Where(t => t.NumeroMoto == numeroMoto && t.IdEtapa == idEtapa && !t.Descartada)
            .OrderBy(t => t.Volta)
            .ThenBy(t => t.IdEspecial)
            .ThenBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<List<Tempo>> GetByInscricaoAsync(int idInscricao)
    {
        return await _context.Tempos
            .Where(t => t.IdInscricao == idInscricao && !t.Descartada)
            .OrderBy(t => t.Volta)
            .ThenBy(t => t.IdEspecial)
            .ThenBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<PagedResult<Tempo>> GetPagedAsync(LeituraFilterParams filter)
    {
        var query = _context.Tempos
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Piloto)
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Categoria)
            .Include(t => t.Dispositivo)
            .AsQueryable();

        // Filtros
        if (filter.IdEtapa.HasValue)
            query = query.Where(t => t.IdEtapa == filter.IdEtapa.Value);

        if (filter.NumeroMoto.HasValue)
            query = query.Where(t => t.NumeroMoto == filter.NumeroMoto.Value);

        if (!string.IsNullOrEmpty(filter.Tipo))
            query = query.Where(t => t.Tipo == filter.Tipo);

        if (filter.IdEspecial.HasValue)
            query = query.Where(t => t.IdEspecial == filter.IdEspecial.Value);

        if (filter.Volta.HasValue)
            query = query.Where(t => t.Volta == filter.Volta.Value);

        if (filter.IdDispositivo.HasValue)
            query = query.Where(t => t.IdDispositivo == filter.IdDispositivo.Value);

        if (filter.DataInicio.HasValue)
            query = query.Where(t => t.Timestamp >= filter.DataInicio.Value);

        if (filter.DataFim.HasValue)
            query = query.Where(t => t.Timestamp <= filter.DataFim.Value);

        if (filter.Descartada.HasValue)
            query = query.Where(t => t.Descartada == filter.Descartada.Value);

        if (filter.Sincronizado.HasValue)
            query = query.Where(t => t.Sincronizado == filter.Sincronizado.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Tempo>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    #endregion

    #region ENDURO

    public async Task<Tempo?> GetEntradaEspecialAsync(int idEtapa, int numeroMoto, int idEspecial, int volta)
    {
        return await _context.Tempos
            .Where(t => t.IdEtapa == idEtapa
                     && t.NumeroMoto == numeroMoto
                     && t.IdEspecial == idEspecial
                     && t.Volta == volta
                     && t.Tipo == "E"
                     && !t.Descartada)
            .OrderBy(t => t.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<Tempo?> GetSaidaEspecialAsync(int idEtapa, int numeroMoto, int idEspecial, int volta)
    {
        return await _context.Tempos
            .Where(t => t.IdEtapa == idEtapa
                     && t.NumeroMoto == numeroMoto
                     && t.IdEspecial == idEspecial
                     && t.Volta == volta
                     && t.Tipo == "S"
                     && !t.Descartada)
            .OrderBy(t => t.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Tempo>> GetTemposEspecialAsync(int idEtapa, int idEspecial, int volta)
    {
        return await _context.Tempos
            .Include(t => t.Inscricao)
                .ThenInclude(i => i.Piloto)
            .Where(t => t.IdEtapa == idEtapa
                     && t.IdEspecial == idEspecial
                     && t.Volta == volta
                     && !t.Descartada)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }

    #endregion

    #region CIRCUITO

    public async Task<Tempo?> GetUltimaPassagemAsync(int idEtapa, int numeroMoto)
    {
        return await _context.Tempos
            .Where(t => t.IdEtapa == idEtapa
                     && t.NumeroMoto == numeroMoto
                     && t.Tipo == "P"
                     && !t.Descartada)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Tempo>> GetPassagensAsync(int idEtapa, int numeroMoto)
    {
        return await _context.Tempos
            .Where(t => t.IdEtapa == idEtapa
                     && t.NumeroMoto == numeroMoto
                     && t.Tipo == "P"
                     && !t.Descartada)
            .OrderBy(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<int> GetTotalVoltasAsync(int idEtapa, int numeroMoto)
    {
        return await _context.Tempos
            .CountAsync(t => t.IdEtapa == idEtapa
                          && t.NumeroMoto == numeroMoto
                          && t.Tipo == "P"
                          && !t.Descartada);
    }

    #endregion

    #region Verificações

    public async Task<bool> ExisteLeituraAsync(string hashLeitura)
    {
        return await _context.Tempos
            .AnyAsync(t => t.HashLeitura == hashLeitura);
    }

    public async Task<bool> ExisteLeituraSimilarAsync(
        int idEtapa,
        int numeroMoto,
        DateTime timestamp,
        string tipo,
        int toleranciaMs = 1000)
    {
        var minTime = timestamp.AddMilliseconds(-toleranciaMs);
        var maxTime = timestamp.AddMilliseconds(toleranciaMs);

        return await _context.Tempos
            .AnyAsync(t => t.IdEtapa == idEtapa
                        && t.NumeroMoto == numeroMoto
                        && t.Tipo == tipo
                        && t.Timestamp >= minTime
                        && t.Timestamp <= maxTime
                        && !t.Descartada);
    }

    #endregion

    #region CRUD

    public async Task<Tempo> AddAsync(Tempo tempo)
    {
        await _context.Tempos.AddAsync(tempo);
        await _context.SaveChangesAsync();
        return tempo;
    }

    public async Task AddRangeAsync(List<Tempo> tempos)
    {
        await _context.Tempos.AddRangeAsync(tempos);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tempo tempo)
    {
        tempo.DataAtualizacao = DateTime.UtcNow;
        _context.Tempos.Update(tempo);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(List<Tempo> tempos)
    {
        foreach (var tempo in tempos)
        {
            tempo.DataAtualizacao = DateTime.UtcNow;
        }
        _context.Tempos.UpdateRange(tempos);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Estatísticas

    public async Task<int> CountByEtapaAsync(int idEtapa)
    {
        return await _context.Tempos
            .CountAsync(t => t.IdEtapa == idEtapa && !t.Descartada);
    }

    public async Task<int> CountByDispositivoAsync(int idDispositivo)
    {
        return await _context.Tempos
            .CountAsync(t => t.IdDispositivo == idDispositivo);
    }

    public async Task<DateTime?> GetUltimaLeituraEtapaAsync(int idEtapa)
    {
        return await _context.Tempos
            .Where(t => t.IdEtapa == idEtapa && !t.Descartada)
            .MaxAsync(t => (DateTime?)t.Timestamp);
    }

    #endregion


    /// <summary>
    /// Busca todos os tempos de uma etapa otimizado para cálculo de resultados
    /// </summary>
    public async Task<List<Tempo>> GetByEtapaParaResultadoAsync(int idEtapa)
    {
        return await _context.Tempos
            .AsNoTracking() // Leitura apenas, sem tracking
            .Include(t => t.Inscricao)
            .ThenInclude(i => i.Piloto)
            .Include(t => t.Inscricao)
            .ThenInclude(i => i.Categoria)
            .Where(t => t.IdEtapa == idEtapa && !t.Descartada)
            .OrderBy(t => t.NumeroMoto)
            .ThenBy(t => t.Volta)
            .ThenBy(t => t.IdEspecial)
            .ThenBy(t => t.Tipo)
            .ToListAsync();
    }

    /// <summary>
    /// Busca tempos agrupados por piloto (mais eficiente para grandes volumes)
    /// </summary>
    public async Task<Dictionary<int, List<Tempo>>> GetTemposAgrupadosPorMotoAsync(int idEtapa)
    {
        var tempos = await _context.Tempos
            .AsNoTracking()
            .Where(t => t.IdEtapa == idEtapa && !t.Descartada)
            .ToListAsync();

        return tempos.GroupBy(t => t.NumeroMoto)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Busca resumo de tempos (sem detalhes) para classificação rápida
    /// </summary>
    public async Task<List<ResumoTempoDto>> GetResumoTemposAsync(int idEtapa)
    {
        return await _context.Tempos
            .AsNoTracking()
            .Where(t => t.IdEtapa == idEtapa && !t.Descartada && t.Tipo == "S")
            .GroupBy(t => new { t.NumeroMoto, t.IdInscricao })
            .Select(g => new ResumoTempoDto
            {
                NumeroMoto = g.Key.NumeroMoto,
                IdInscricao = g.Key.IdInscricao,
                TotalTempoSegundos = g.Sum(t => t.TempoCalculadoSegundos ?? 0),
                TotalEspeciais = g.Count(t => t.TempoCalculadoSegundos != null),
                MelhorTempoSegundos = g.Min(t => t.TempoCalculadoSegundos)
            })
            .OrderBy(r => r.TotalTempoSegundos)
            .ToListAsync();
    }
}
