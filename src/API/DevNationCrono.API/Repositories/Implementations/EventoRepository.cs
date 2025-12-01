using DevNationCrono.API.Data;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class EventoRepository : IEventoRepository
{
    private readonly ApplicationDbContext _context;

    public EventoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Evento?> GetByIdAsync(int id)
    {
        return await _context.Eventos
            .Include(e => e.Modalidade)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Evento?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Eventos
            .Include(e => e.Modalidade)
            .Include(e => e.Etapas.Where(et => et.Status != "CANCELADA"))
            .Include(e => e.Categorias.Where(c => c.Ativo))
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Evento>> GetAllAsync()
    {
        return await _context.Eventos
            .Include(e => e.Modalidade)
            .OrderByDescending(e => e.DataInicio)
            .ToListAsync();
    }

    public async Task<List<Evento>> GetActivesAsync()
    {
        return await _context.Eventos
            .Include(e => e.Modalidade)
            .Where(e => e.Ativo)
            .OrderByDescending(e => e.DataInicio)
            .ToListAsync();
    }

    public async Task<PagedResult<Evento>> GetPagedAsync(EventoFilterParams filter)
    {
        var query = _context.Eventos
            .Include(e => e.Modalidade)
            .AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(filter.Nome))
        {
            query = query.Where(e => e.Nome.Contains(filter.Nome));
        }

        if (!string.IsNullOrWhiteSpace(filter.Cidade))
        {
            query = query.Where(e => e.Cidade.Contains(filter.Cidade));
        }

        if (!string.IsNullOrWhiteSpace(filter.Uf))
        {
            query = query.Where(e => e.Uf == filter.Uf.ToUpper());
        }

        if (filter.IdModalidade.HasValue)
        {
            query = query.Where(e => e.IdModalidade == filter.IdModalidade.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(e => e.Status == filter.Status);
        }

        if (filter.InscricoesAbertas.HasValue)
        {
            query = query.Where(e => e.InscricoesAbertas == filter.InscricoesAbertas.Value);
        }

        if (filter.DataInicio.HasValue)
        {
            query = query.Where(e => e.DataInicio >= filter.DataInicio.Value);
        }

        if (filter.DataFim.HasValue)
        {
            query = query.Where(e => e.DataFim <= filter.DataFim.Value);
        }

        if (filter.Ativo.HasValue)
        {
            query = query.Where(e => e.Ativo == filter.Ativo.Value);
        }

        // Contar total
        var totalCount = await query.CountAsync();

        // Aplicar ordenação e paginação
        var items = await query
            .OrderByDescending(e => e.DataInicio)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Evento>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<List<Evento>> GetByModalidadeAsync(int idModalidade)
    {
        return await _context.Eventos
            .Include(e => e.Modalidade)
            .Where(e => e.IdModalidade == idModalidade && e.Ativo)
            .OrderByDescending(e => e.DataInicio)
            .ToListAsync();
    }

    public async Task<List<Evento>> GetProximosAsync(int quantidade = 5)
    {
        return await _context.Eventos
            .Include(e => e.Modalidade)
            .Where(e => e.Ativo && e.DataInicio >= DateTime.UtcNow.Date)
            .OrderBy(e => e.DataInicio)
            .Take(quantidade)
            .ToListAsync();
    }

    public async Task<Evento> AddAsync(Evento evento)
    {
        await _context.Eventos.AddAsync(evento);
        await _context.SaveChangesAsync();
        return evento;
    }

    public async Task UpdateAsync(Evento evento)
    {
        evento.DataAtualizacao = DateTime.UtcNow;
        _context.Eventos.Update(evento);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var evento = await GetByIdAsync(id);
        if (evento != null)
        {
            evento.Ativo = false;
            await UpdateAsync(evento);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Eventos.AnyAsync(e => e.Id == id);
    }

    public async Task<int> CountEtapasAsync(int id)
    {
        return await _context.Etapas
            .CountAsync(et => et.IdEvento == id && et.Status != "CANCELADA");
    }

    public async Task<int> CountCategoriasAsync(int id)
    {
        return await _context.Categorias
            .CountAsync(c => c.IdEvento == id && c.Ativo);
    }

    public async Task<int> CountInscritosAsync(int id)
    {
        return await _context.Inscricoes
            .CountAsync(i => i.IdEvento == id && i.StatusPagamento != "CANCELADO");
    }
}
