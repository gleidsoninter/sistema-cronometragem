using AppColetor.Models.DTOs;
using AppColetor.Models.Entities;

namespace AppColetor.Services.Interfaces
{
    public interface IApiService
    {
        /// <summary>
        /// Indica se está conectado à API
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indica se está autenticado
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Evento disparado quando status da conexão muda
        /// </summary>
        event EventHandler<ApiStatusEventArgs>? StatusChanged;

        /// <summary>
        /// Configura a URL base da API
        /// </summary>
        void ConfigurarUrl(string baseUrl);

        /// <summary>
        /// Configura o token de autenticação
        /// </summary>
        void ConfigurarToken(string token);

        /// <summary>
        /// Verifica se a API está acessível
        /// </summary>
        Task<bool> VerificarConexaoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Autentica o dispositivo coletor
        /// </summary>
        Task<AuthResultDto?> AutenticarDispositivoAsync(string deviceId, string senha, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia uma única leitura para a API
        /// </summary>
        Task<LeituraResponseDto?> EnviarLeituraAsync(Leitura leitura, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia um lote de leituras para a API
        /// </summary>
        Task<LoteResponseDto?> EnviarLoteAsync(IEnumerable<Leitura> leituras, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sincroniza leituras pendentes
        /// </summary>
        Task<List<Leitura>> SincronizarLeiturasAsync(IEnumerable<Leitura> leituras, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém informações da etapa
        /// </summary>
        Task<EtapaInfoDto?> ObterEtapaAsync(int idEtapa, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia heartbeat para indicar que o coletor está ativo
        /// </summary>
        Task<bool> EnviarHeartbeatAsync(string deviceId, CancellationToken cancellationToken = default);
    }

    public class ApiStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? Message { get; set; }
        public int? StatusCode { get; set; }
    }
}