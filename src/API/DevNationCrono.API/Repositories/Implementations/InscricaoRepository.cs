using DevNationCrono.API.Data;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class InscricaoRepository : IInscricaoRepository
{
    private readonly ApplicationDbContext _context;

    public InscricaoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    #region Buscar

    public async Task<Inscricao?> GetByIdAsync(int id)
    {
        return await _context.Inscricoes
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Inscricao?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Inscricoes
            .Include(i => i.Piloto)
            .Include(i => i.Evento)
                .ThenInclude(e => e.Modalidade)
            .Include(i => i.Categoria)
            .Include(i => i.Etapa)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<List<Inscricao>> GetByEventoAsync(int idEvento)
    {
        return await _context.Inscricoes
            .Include(i => i.Piloto)
            .Include(i => i.Categoria)
            .Include(i => i.Etapa)
            .Where(i => i.IdEvento == idEvento && i.Ativo)
            .OrderBy(i => i.NumeroMoto)
            .ToListAsync();
    }

    public async Task<List<Inscricao>> GetByEtapaAsync(int idEtapa)
    {
        return await _context.Inscricoes
            .Include(i => i.Piloto)
            .Include(i => i.Categoria)
            .Where(i => i.IdEtapa == idEtapa && i.Ativo)
            .OrderBy(i => i.NumeroMoto)
            .ToListAsync();
    }

    public async Task<List<Inscricao>> GetByCategoriaAsync(int idCategoria)
    {
        return await _context.Inscricoes
            .Include(i => i.Piloto)
            .Include(i => i.Etapa)
            .Where(i => i.IdCategoria == idCategoria && i.Ativo)
            .OrderBy(i => i.NumeroMoto)
            .ToListAsync();
    }

    public async Task<List<Inscricao>> GetByPilotoAsync(int idPiloto)
    {
        return await _context.Inscricoes
            .Include(i => i.Evento)
            .Include(i => i.Categoria)
            .Include(i => i.Etapa)
            .Where(i => i.IdPiloto == idPiloto && i.Ativo)
            .OrderByDescending(i => i.DataInscricao)
            .ToListAsync();
    }

    public async Task<List<Inscricao>> GetByPilotoEventoAsync(int idPiloto, int idEvento)
    {
        return await _context.Inscricoes
            .Include(i => i.Categoria)
            .Include(i => i.Etapa)
            .Where(i => i.IdPiloto == idPiloto && i.IdEvento == idEvento && i.Ativo)
            .OrderBy(i => i.DataInscricao)
            .ToListAsync();
    }

    public async Task<PagedResult<Inscricao>> GetPagedAsync(InscricaoFilterParams filter)
    {
        var query = _context.Inscricoes
            .Include(i => i.Piloto)
            .Include(i => i.Evento)
            .Include(i => i.Categoria)
            .Include(i => i.Etapa)
            .AsQueryable();

        // Filtros
        if (filter.IdEvento.HasValue)
            query = query.Where(i => i.IdEvento == filter.IdEvento.Value);

        if (filter.IdEtapa.HasValue)
            query = query.Where(i => i.IdEtapa == filter.IdEtapa.Value);

        if (filter.IdCategoria.HasValue)
            query = query.Where(i => i.IdCategoria == filter.IdCategoria.Value);

        if (filter.IdPiloto.HasValue)
            query = query.Where(i => i.IdPiloto == filter.IdPiloto.Value);

        if (filter.NumeroMoto.HasValue)
            query = query.Where(i => i.NumeroMoto == filter.NumeroMoto.Value);

        if (!string.IsNullOrWhiteSpace(filter.StatusPagamento))
            query = query.Where(i => i.StatusPagamento == filter.StatusPagamento);

        if (!string.IsNullOrWhiteSpace(filter.NomePiloto))
            query = query.Where(i => i.Piloto.Nome.Contains(filter.NomePiloto));

        if (filter.Ativo.HasValue)
            query = query.Where(i => i.Ativo == filter.Ativo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.DataInscricao)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Inscricao>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    #endregion

    #region Verificações

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Inscricoes.AnyAsync(i => i.Id == id);
    }

    public async Task<bool> JaInscritoNaCategoriaAsync(int idPiloto, int idCategoria, int idEtapa)
    {
        return await _context.Inscricoes
            .AnyAsync(i => i.IdPiloto == idPiloto
                        && i.IdCategoria == idCategoria
                        && i.IdEtapa == idEtapa
                        && i.Ativo
                        && i.StatusPagamento != "CANCELADO");
    }

    public async Task<bool> JaInscritoNoEventoAsync(int idPiloto, int idEvento)
    {
        return await _context.Inscricoes
            .AnyAsync(i => i.IdPiloto == idPiloto
                        && i.IdEvento == idEvento
                        && i.Ativo
                        && i.StatusPagamento != "CANCELADO");
    }

    public async Task<int> ContarInscricoesPilotoEventoAsync(int idPiloto, int idEvento)
    {
        return await _context.Inscricoes
            .CountAsync(i => i.IdPiloto == idPiloto
                          && i.IdEvento == idEvento
                          && i.Ativo
                          && i.StatusPagamento != "CANCELADO");
    }

    public async Task<bool> NumeroMotoEmUsoAsync(int numeroMoto, int idEvento, int? excludeIdInscricao = null)
    {
        var query = _context.Inscricoes
            .Where(i => i.NumeroMoto == numeroMoto
                     && i.IdEvento == idEvento
                     && i.Ativo
                     && i.StatusPagamento != "CANCELADO");

        if (excludeIdInscricao.HasValue)
            query = query.Where(i => i.Id != excludeIdInscricao.Value);

        return await query.AnyAsync();
    }

    #endregion

    #region Número de Moto

    public async Task<int> GetProximoNumeroMotoAsync(int idEvento)
    {
        var maxNumero = await _context.Inscricoes
            .Where(i => i.IdEvento == idEvento && i.Ativo)
            .MaxAsync(i => (int?)i.NumeroMoto) ?? 0;

        return maxNumero + 1;
    }

    public async Task<int?> GetNumeroMotoPilotoEventoAsync(int idPiloto, int idEvento)
    {
        var inscricao = await _context.Inscricoes
            .Where(i => i.IdPiloto == idPiloto
                     && i.IdEvento == idEvento
                     && i.Ativo
                     && i.StatusPagamento != "CANCELADO")
            .FirstOrDefaultAsync();

        return inscricao?.NumeroMoto;
    }

    #endregion

    #region CRUD

    public async Task<Inscricao> AddAsync(Inscricao inscricao)
    {
        await _context.Inscricoes.AddAsync(inscricao);
        await _context.SaveChangesAsync();
        return inscricao;
    }

    public async Task AddRangeAsync(List<Inscricao> inscricoes)
    {
        await _context.Inscricoes.AddRangeAsync(inscricoes);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Inscricao inscricao)
    {
        inscricao.DataAtualizacao = DateTime.UtcNow;
        _context.Inscricoes.Update(inscricao);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var inscricao = await GetByIdAsync(id);
        if (inscricao != null)
        {
            inscricao.Ativo = false;
            inscricao.StatusPagamento = "CANCELADO";
            await UpdateAsync(inscricao);
        }
    }

    #endregion

    #region Estatísticas

    public async Task<int> CountByEventoAsync(int idEvento)
    {
        return await _context.Inscricoes
            .CountAsync(i => i.IdEvento == idEvento
                          && i.Ativo
                          && i.StatusPagamento != "CANCELADO");
    }

    public async Task<int> CountByCategoriaAsync(int idCategoria)
    {
        return await _context.Inscricoes
            .CountAsync(i => i.IdCategoria == idCategoria
                          && i.Ativo
                          && i.StatusPagamento != "CANCELADO");
    }

    public async Task<int> CountPagosEventoAsync(int idEvento)
    {
        return await _context.Inscricoes
            .CountAsync(i => i.IdEvento == idEvento
                          && i.Ativo
                          && i.StatusPagamento == "PAGO");
    }

    public async Task<decimal> SomarValorPagosEventoAsync(int idEvento)
    {
        return await _context.Inscricoes
            .Where(i => i.IdEvento == idEvento
                     && i.Ativo
                     && i.StatusPagamento == "PAGO")
            .SumAsync(i => i.ValorFinal);
    }

    #endregion
}
