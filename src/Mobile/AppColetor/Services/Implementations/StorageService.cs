using AppColetor.Data;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class StorageService : IStorageService
    {
        private readonly AppDatabase _database;

        public StorageService(AppDatabase database)
        {
            _database = database;
        }

        public async Task<int> SalvarLeituraAsync(Leitura leitura)
        {
            // Verificar duplicata
            if (!string.IsNullOrEmpty(leitura.Hash))
            {
                if (await _database.ExisteLeituraComHashAsync(leitura.Hash))
                {
                    System.Diagnostics.Debug.WriteLine($"[Storage] Leitura duplicada ignorada: {leitura.Hash}");
                    return 0;
                }
            }

            return await _database.SalvarLeituraAsync(leitura);
        }

        public Task<List<Leitura>> GetLeiturasRecentesAsync(int limite = 100)
            => _database.GetLeiturasAsync(limite);

        public Task<List<Leitura>> GetLeiturasNaoSincronizadasAsync()
            => _database.GetLeiturasNaoSincronizadasAsync();

        public Task<int> ContarLeiturasAsync()
            => _database.ContarLeiturasAsync();

        public Task<int> ContarLeiturasNaoSincronizadasAsync()
            => _database.ContarLeiturasNaoSincronizadasAsync();

        public Task MarcarComoSincronizadaAsync(long id)
            => _database.MarcarComoSincronizadaAsync(id);

        public Task<bool> ExisteLeituraAsync(string hash)
            => _database.ExisteLeituraComHashAsync(hash);
    }
}