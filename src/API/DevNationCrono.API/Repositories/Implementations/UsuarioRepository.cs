using DevNationCrono.API.Data;
using DevNationCrono.API.Models.Entities;
using DevNationCrono.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace DevNationCrono.API.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ApplicationDbContext _context;

    public UsuarioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> GetByEmailAsync(string email)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Ativo);
    }

    public async Task<List<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios
            .Where(u => u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<List<Usuario>> GetByRoleAsync(string role)
    {
        return await _context.Usuarios
            .Where(u => u.Role == role && u.Ativo)
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var query = _context.Usuarios.Where(u => u.Email.ToLower() == email.ToLower());

        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<Usuario> AddAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> UpdateAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task DeleteAsync(int id)
    {
        var usuario = await GetByIdAsync(id);
        if (usuario != null)
        {
            usuario.Ativo = false;  // Soft delete
            await _context.SaveChangesAsync();
        }
    }
}
