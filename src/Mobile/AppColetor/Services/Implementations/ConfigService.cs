using AppColetor.Data;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class ConfigService : IConfigService
    {
        private readonly AppDatabase _database;

        public ConfigService(AppDatabase database)
        {
            _database = database;
        }

        public async Task<string> GetStringAsync(string chave, string valorPadrao = "")
        {
            var valor = await _database.GetConfigAsync(chave);
            return valor ?? valorPadrao;
        }

        public async Task<int> GetIntAsync(string chave, int valorPadrao = 0)
        {
            var valor = await _database.GetConfigAsync(chave);
            return int.TryParse(valor, out int result) ? result : valorPadrao;
        }

        public async Task<bool> GetBoolAsync(string chave, bool valorPadrao = false)
        {
            var valor = await _database.GetConfigAsync(chave);
            return bool.TryParse(valor, out bool result) ? result : valorPadrao;
        }

        public Task SetStringAsync(string chave, string valor)
            => _database.SetConfigAsync(chave, valor);

        public Task SetIntAsync(string chave, int valor)
            => _database.SetConfigAsync(chave, valor.ToString());

        public Task SetBoolAsync(string chave, bool valor)
            => _database.SetConfigAsync(chave, valor.ToString());
    }
}