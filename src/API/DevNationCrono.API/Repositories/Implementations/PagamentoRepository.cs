using DevNationCrono.API.Data;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevNationCrono.API.Repositories.Implementations;

public class PagamentoRepository : IPagamentoRepository
{
    private readonly ApplicationDbContext _context;

    public PagamentoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Pagamento?> GetByIdAsync(int id)
    {
        return await _context.Pagamentos
            .Include(p => p.Inscricao)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pagamento?> GetPendenteByInscricaoAsync(int idInscricao)
    {
        return await _context.Pagamentos
            .Where(p => p.IdInscricao == idInscricao)
            .Where(p => p.Status == StatusPagamentoPix.Pendente ||
                       p.Status == StatusPagamentoPix.Aguardando)
            .OrderByDescending(p => p.DataCriacao)
            .FirstOrDefaultAsync();
    }

    public async Task<Pagamento?> GetUltimoByInscricaoAsync(int idInscricao)
    {
        return await _context.Pagamentos
            .Where(p => p.IdInscricao == idInscricao)
            .OrderByDescending(p => p.DataCriacao)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Pagamento>> GetByIdExternoAsync(string idExterno)
    {
        return await _context.Pagamentos
            .Where(p => p.IdExterno == idExterno)
            .ToListAsync();
    }

    public async Task<List<Pagamento>> GetByInscricaoAsync(int idInscricao)
    {
        return await _context.Pagamentos
            .Where(p => p.IdInscricao == idInscricao)
            .OrderByDescending(p => p.DataCriacao)
            .ToListAsync();
    }

    public async Task<List<Pagamento>> GetExpiradasAsync()
    {
        return await _context.Pagamentos
            .Where(p => (p.Status == StatusPagamentoPix.Pendente ||
                        p.Status == StatusPagamentoPix.Aguardando))
            .Where(p => p.DataExpiracao < DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<Pagamento> AddAsync(Pagamento pagamento)
    {
        await _context.Pagamentos.AddAsync(pagamento);
        await _context.SaveChangesAsync();
        return pagamento;
    }

    public async Task UpdateAsync(Pagamento pagamento)
    {
        pagamento.DataAtualizacao = DateTime.UtcNow;
        _context.Pagamentos.Update(pagamento);
        await _context.SaveChangesAsync();
    }
}
