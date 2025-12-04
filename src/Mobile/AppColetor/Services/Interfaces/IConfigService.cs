namespace AppColetor.Services.Interfaces
{
    public interface IConfigService
    {
        Task<string> GetStringAsync(string chave, string valorPadrao = "");
        Task<int> GetIntAsync(string chave, int valorPadrao = 0);
        Task<bool> GetBoolAsync(string chave, bool valorPadrao = false);
        Task SetStringAsync(string chave, string valor);
        Task SetIntAsync(string chave, int valor);
        Task SetBoolAsync(string chave, bool valor);
    }
}