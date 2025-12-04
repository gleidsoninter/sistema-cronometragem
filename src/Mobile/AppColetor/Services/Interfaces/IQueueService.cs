using AppColetor.Models.Entities;

namespace AppColetor.Services.Interfaces
{
    public interface IQueueService
    {
        /// <summary>
        /// Total de itens na fila
        /// </summary>
        int TotalNaFila { get; }

        /// <summary>
        /// Indica se a fila está processando
        /// </summary>
        bool IsProcessando { get; }

        /// <summary>
        /// Evento disparado quando item é processado
        /// </summary>
        event EventHandler<QueueItemEventArgs>? ItemProcessado;

        /// <summary>
        /// Evento disparado quando fila é esvaziada
        /// </summary>
        event EventHandler? FilaEsvaziada;

        /// <summary>
        /// Adiciona uma leitura à fila
        /// </summary>
        Task<int> EnfileirarAsync(Leitura leitura, int prioridade = 0);

        /// <summary>
        /// Adiciona múltiplas leituras à fila
        /// </summary>
        Task EnfileirarLoteAsync(IEnumerable<Leitura> leituras, int prioridade = 0);

        /// <summary>
        /// Obtém próximos itens para processar
        /// </summary>
        Task<List<FilaSincronizacao>> ObterProximosAsync(int quantidade = 50);

        /// <summary>
        /// Marca item como concluído
        /// </summary>
        Task MarcarConcluidoAsync(int idFila);

        /// <summary>
        /// Marca item com erro para retry
        /// </summary>
        Task MarcarErroAsync(int idFila, string erro);

        /// <summary>
        /// Remove itens concluídos
        /// </summary>
        Task LimparConcluidosAsync();

        /// <summary>
        /// Obtém estatísticas da fila
        /// </summary>
        Task<QueueStats> ObterEstatisticasAsync();
    }

    public class QueueItemEventArgs : EventArgs
    {
        public int IdFila { get; set; }
        public int IdLeitura { get; set; }
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
    }

    public class QueueStats
    {
        public int TotalPendentes { get; set; }
        public int TotalProcessando { get; set; }
        public int TotalConcluidos { get; set; }
        public int TotalComErro { get; set; }
        public DateTime? UltimoProcessamento { get; set; }
    }
}