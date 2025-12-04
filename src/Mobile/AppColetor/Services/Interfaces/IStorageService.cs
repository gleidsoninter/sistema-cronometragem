using AppColetor.Models.Entities;

namespace AppColetor.Services.Interfaces
{
    public interface IStorageService
    {
        Task<int> SalvarLeituraAsync(Leitura leitura);
        Task<List<Leitura>> GetLeiturasRecentesAsync(int limite = 100);
        Task<List<Leitura>> GetLeiturasNaoSincronizadasAsync();
        Task<int> ContarLeiturasAsync();
        Task<int> ContarLeiturasNaoSincronizadasAsync();
        Task MarcarComoSincronizadaAsync(long id);
        Task<bool> ExisteLeituraAsync(string hash);
    }
}