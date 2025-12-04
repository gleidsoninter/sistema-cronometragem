using AppColetor.Data;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class QueueService : IQueueService
    {
        private readonly AppDatabase _database;
        private readonly SemaphoreSlim _queueLock = new(1, 1);
        private int _totalNaFila;
        private bool _isProcessando;

        public event EventHandler<QueueItemEventArgs>? ItemProcessado;
        public event EventHandler? FilaEsvaziada;

        public int TotalNaFila => _totalNaFila;
        public bool IsProcessando => _isProcessando;

        public QueueService(AppDatabase database)
        {
            _database = database;

            // Carregar contagem inicial
            _ = AtualizarContagemAsync();
        }

        private async Task AtualizarContagemAsync()
        {
            var db = await _database.GetDatabaseAsync();

            _totalNaFila = await db.Table<FilaSincronizacao>()
                .Where(f => f.Status == "PENDENTE" || f.Status == "EM_PROCESSAMENTO")
                .CountAsync();
        }

        public async Task<int> EnfileirarAsync(Leitura leitura, int prioridade = 0)
        {
            await _queueLock.WaitAsync();

            try
            {
                var db = await _database.GetDatabaseAsync();

                // Verificar se já não está na fila
                var existente = await db.Table<FilaSincronizacao>()
                    .Where(f => f.IdLeitura == leitura.Id && f.Status != "CONCLUIDO")
                    .FirstOrDefaultAsync();

                if (existente != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Queue] Leitura {leitura.Id} já está na fila");
                    return existente.Id;
                }

                var item = new FilaSincronizacao
                {
                    IdLeitura = leitura.Id,
                    Status = "PENDENTE",
                    Prioridade = prioridade,
                    DataCriacao = DateTime.UtcNow
                };

                await db.InsertAsync(item);

                _totalNaFila++;

                System.Diagnostics.Debug.WriteLine(
                    $"[Queue] Leitura {leitura.Id} enfileirada. Total: {_totalNaFila}");

                return item.Id;
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public async Task EnfileirarLoteAsync(IEnumerable<Leitura> leituras, int prioridade = 0)
        {
            await _queueLock.WaitAsync();

            try
            {
                var db = await _database.GetDatabaseAsync();

                var itens = new List<FilaSincronizacao>();

                foreach (var leitura in leituras)
                {
                    // Verificar se já não está na fila
                    var existente = await db.Table<FilaSincronizacao>()
                        .Where(f => f.IdLeitura == leitura.Id && f.Status != "CONCLUIDO")
                        .CountAsync();

                    if (existente > 0)
                        continue;

                    itens.Add(new FilaSincronizacao
                    {
                        IdLeitura = leitura.Id,
                        Status = "PENDENTE",
                        Prioridade = prioridade,
                        DataCriacao = DateTime.UtcNow
                    });
                }

                if (itens.Count > 0)
                {
                    await db.InsertAllAsync(itens);
                    _totalNaFila += itens.Count;

                    System.Diagnostics.Debug.WriteLine(
                        $"[Queue] {itens.Count} leituras enfileiradas. Total: {_totalNaFila}");
                }
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public async Task<List<FilaSincronizacao>> ObterProximosAsync(int quantidade = 50)
        {
            var db = await _database.GetDatabaseAsync();

            var agora = DateTime.UtcNow;

            // Buscar itens pendentes ou que já podem ser retentados
            var itens = await db.Table<FilaSincronizacao>()
                .Where(f =>
                    f.Status == "PENDENTE" ||
                    (f.Status == "EM_PROCESSAMENTO" && f.ProximaTentativa != null && f.ProximaTentativa <= agora))
                .OrderByDescending(f => f.Prioridade)
                .ThenBy(f => f.DataCriacao) // FIFO dentro da mesma prioridade
                .Take(quantidade)
                .ToListAsync();

            // Marcar como em processamento
            foreach (var item in itens)
            {
                item.Status = "EM_PROCESSAMENTO";
                item.UltimaTentativa = agora;
                await db.UpdateAsync(item);
            }

            return itens;
        }

        public async Task MarcarConcluidoAsync(int idFila)
        {
            await _queueLock.WaitAsync();

            try
            {
                var db = await _database.GetDatabaseAsync();

                var item = await db.Table<FilaSincronizacao>()
                    .Where(f => f.Id == idFila)
                    .FirstOrDefaultAsync();

                if (item != null)
                {
                    item.Status = "CONCLUIDO";
                    item.DataConclusao = DateTime.UtcNow;
                    await db.UpdateAsync(item);

                    _totalNaFila = Math.Max(0, _totalNaFila - 1);

                    ItemProcessado?.Invoke(this, new QueueItemEventArgs
                    {
                        IdFila = idFila,
                        IdLeitura = item.IdLeitura,
                        Sucesso = true
                    });

                    // Verificar se fila esvaziou
                    if (_totalNaFila == 0)
                    {
                        FilaEsvaziada?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public async Task MarcarErroAsync(int idFila, string erro)
        {
            await _queueLock.WaitAsync();

            try
            {
                var db = await _database.GetDatabaseAsync();

                var item = await db.Table<FilaSincronizacao>()
                    .Where(f => f.Id == idFila)
                    .FirstOrDefaultAsync();

                if (item != null)
                {
                    item.Tentativas++;
                    item.UltimoErro = erro;
                    item.UltimaTentativa = DateTime.UtcNow;

                    if (item.Tentativas >= item.MaxTentativas)
                    {
                        // Atingiu máximo de tentativas
                        item.Status = "ERRO";
                        _totalNaFila = Math.Max(0, _totalNaFila - 1);

                        System.Diagnostics.Debug.WriteLine(
                            $"[Queue] Item {idFila} atingiu máximo de tentativas");
                    }
                    else
                    {
                        // Calcular próxima tentativa com backoff
                        var delaySegundos = Math.Pow(2, item.Tentativas) * 5; // 5s, 10s, 20s, 40s, 80s
                        item.ProximaTentativa = DateTime.UtcNow.AddSeconds(delaySegundos);
                        item.Status = "PENDENTE";

                        System.Diagnostics.Debug.WriteLine(
                            $"[Queue] Item {idFila} agendado para retry em {delaySegundos}s");
                    }

                    await db.UpdateAsync(item);

                    ItemProcessado?.Invoke(this, new QueueItemEventArgs
                    {
                        IdFila = idFila,
                        IdLeitura = item.IdLeitura,
                        Sucesso = false,
                        Erro = erro
                    });
                }
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public async Task LimparConcluidosAsync()
        {
            var db = await _database.GetDatabaseAsync();

            var deletados = await db.ExecuteAsync(@"
                DELETE FROM fila_sincronizacao 
                WHERE Status = 'CONCLUIDO'");

            System.Diagnostics.Debug.WriteLine($"[Queue] {deletados} itens concluídos removidos");
        }

        public async Task<QueueStats> ObterEstatisticasAsync()
        {
            var db = await _database.GetDatabaseAsync();

            var stats = new QueueStats
            {
                TotalPendentes = await db.Table<FilaSincronizacao>()
                    .Where(f => f.Status == "PENDENTE")
                    .CountAsync(),

                TotalProcessando = await db.Table<FilaSincronizacao>()
                    .Where(f => f.Status == "EM_PROCESSAMENTO")
                    .CountAsync(),

                TotalConcluidos = await db.Table<FilaSincronizacao>()
                    .Where(f => f.Status == "CONCLUIDO")
                    .CountAsync(),

                TotalComErro = await db.Table<FilaSincronizacao>()
                    .Where(f => f.Status == "ERRO")
                    .CountAsync()
            };

            var ultimo = await db.Table<FilaSincronizacao>()
                .Where(f => f.DataConclusao != null)
                .OrderByDescending(f => f.DataConclusao)
                .FirstOrDefaultAsync();

            stats.UltimoProcessamento = ultimo?.DataConclusao;

            return stats;
        }
    }
}