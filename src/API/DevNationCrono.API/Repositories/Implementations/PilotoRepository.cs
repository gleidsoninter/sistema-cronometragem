using DevNationCrono.API.Data;
using DevNationCrono.API.Helpers;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Models.Pagination;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class PilotoRepository : IPilotoRepository
{
    private readonly ApplicationDbContext _context;

    public PilotoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Piloto?> GetByIdAsync(int id)
    {
        return await _context.Pilotos
            .Include(p => p.Inscricoes) // Carrega inscrições junto
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Piloto?> GetByCpfAsync(string cpf)
    {
        return await _context.Pilotos
            .FirstOrDefaultAsync(p => p.Cpf == cpf);
    }

    public async Task<Piloto?> GetByEmailAsync(string email)
    {
        return await _context.Pilotos
            .FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<List<Piloto>> GetAllAsync()
    {
        return await _context.Pilotos
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<List<Piloto>> GetActivesAsync()
    {
        return await _context.Pilotos
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<Piloto> AddAsync(Piloto piloto)
    {
        await _context.Pilotos.AddAsync(piloto);
        await _context.SaveChangesAsync();
        return piloto;
    }

    public async Task UpdateAsync(Piloto piloto)
    {
        piloto.DataAtualizacao = DateTime.UtcNow;
        _context.Pilotos.Update(piloto);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var piloto = await GetByIdAsync(id);
        if (piloto != null)
        {
            // Soft delete
            piloto.Ativo = false;
            await UpdateAsync(piloto);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Pilotos.AnyAsync(p => p.Id == id);
    }

    public async Task<bool> CpfExistsAsync(string cpf)
    {
        return await _context.Pilotos.AnyAsync(p => p.Cpf == cpf);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Pilotos.AnyAsync(p => p.Email == email);
    }

    public async Task<PagedResult<Piloto>> GetPagedAsync(PilotoFilterParams filterParams)
    {
        var query = _context.Pilotos.AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(filterParams.Nome))
        {
            query = query.Where(p => p.Nome.Contains(filterParams.Nome));
        }

        if (!string.IsNullOrWhiteSpace(filterParams.Email))
        {
            query = query.Where(p => p.Email.Contains(filterParams.Email));
        }

        if (!string.IsNullOrWhiteSpace(filterParams.Cpf))
        {
            var cpfLimpo = CpfValidator.RemoverFormatacao(filterParams.Cpf);
            query = query.Where(p => p.Cpf == cpfLimpo);
        }

        if (!string.IsNullOrWhiteSpace(filterParams.Cidade))
        {
            query = query.Where(p => p.Cidade.Contains(filterParams.Cidade));
        }

        if (!string.IsNullOrWhiteSpace(filterParams.Uf))
        {
            query = query.Where(p => p.Uf == filterParams.Uf.ToUpper());
        }

        if (filterParams.Ativo.HasValue)
        {
            query = query.Where(p => p.Ativo == filterParams.Ativo.Value);
        }

        // Contar total
        var totalCount = await query.CountAsync();

        // Aplicar paginação
        var items = await query
            .OrderBy(p => p.Nome)
            .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
            .Take(filterParams.PageSize)
            .ToListAsync();

        return new PagedResult<Piloto>(items, totalCount, filterParams.PageNumber, filterParams.PageSize);
    }
}
