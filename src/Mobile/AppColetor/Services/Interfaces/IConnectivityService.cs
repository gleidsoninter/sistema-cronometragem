namespace AppColetor.Services.Interfaces
{
    public interface IConnectivityService
    {
        /// <summary>
        /// Indica se há conexão com a internet
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// Indica se está conectado via WiFi
        /// </summary>
        bool IsWiFi { get; }

        /// <summary>
        /// Indica se está conectado via dados móveis
        /// </summary>
        bool IsCellular { get; }

        /// <summary>
        /// Qualidade estimada da conexão (0-100)
        /// </summary>
        int QualidadeConexao { get; }

        /// <summary>
        /// Tipo de conexão atual
        /// </summary>
        TipoConexao TipoAtual { get; }

        /// <summary>
        /// Evento disparado quando conectividade muda
        /// </summary>
        event EventHandler<ConnectivityEventArgs>? ConnectivityChanged;

        /// <summary>
        /// Verifica se consegue alcançar a API
        /// </summary>
        Task<bool> VerificarAlcanceApiAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Inicia monitoramento contínuo
        /// </summary>
        void IniciarMonitoramento();

        /// <summary>
        /// Para monitoramento
        /// </summary>
        void PararMonitoramento();
    }

    public enum TipoConexao
    {
        Nenhuma,
        WiFi,
        Cellular,
        Ethernet,
        Desconhecido
    }

    public class ConnectivityEventArgs : EventArgs
    {
        public bool IsOnline { get; set; }
        public TipoConexao TipoAnterior { get; set; }
        public TipoConexao TipoAtual { get; set; }
        public string? Mensagem { get; set; }
    }
}