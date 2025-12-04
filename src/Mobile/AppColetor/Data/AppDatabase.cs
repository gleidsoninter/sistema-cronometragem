using SQLite;
using AppColetor.Models.Entities;
using AppColetor.Helpers;

namespace AppColetor.Data
{
    public class AppDatabase : IAsyncDisposable
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTES E CONFIGURAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        private const SQLiteOpenFlags FLAGS =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache |
            SQLiteOpenFlags.FullMutex;

        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        // ═══════════════════════════════════════════════════════════════════
        // INICIALIZAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        private string DatabasePath => Path.Combine(
            FileSystem.AppDataDirectory,
            Constants.DATABASE_NAME);

        public async Task<SQLiteAsyncConnection> GetDatabaseAsync()
        {
            if (_initialized && _database != null)
                return _database;

            await _initLock.WaitAsync();

            try
            {
                if (_initialized && _database != null)
                    return _database;

                _database = new SQLiteAsyncConnection(DatabasePath, FLAGS);

                // Configurações de performance
                await _database.ExecuteAsync("PRAGMA journal_mode=WAL");
                await _database.ExecuteAsync("PRAGMA synchronous=NORMAL");
                await _database.ExecuteAsync("PRAGMA cache_size=10000");
                await _database.ExecuteAsync("PRAGMA temp_store=MEMORY");

                // Criar tabelas
                await _database.CreateTableAsync<Leitura>();
                await _database.CreateTableAsync<Configuracao>();
                await _database.CreateTableAsync<FilaSincronizacao>();
                await _database.CreateTableAsync<LogEvento>();

                // Criar índices adicionais
                await CriarIndicesAsync();

                _initialized = true;

                System.Diagnostics.Debug.WriteLine($"[DB] Banco inicializado: {DatabasePath}");

                return _database;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task CriarIndicesAsync()
        {
            if (_database == null) return;

            // Índices para Leitura
            await _database.ExecuteAsync(@"
                CREATE INDEX IF NOT EXISTS idx_leitura_sincronizado 
                ON leituras (Sincronizado, DataCriacao)");

            await _database.ExecuteAsync(@"
                CREATE INDEX IF NOT EXISTS idx_leitura_etapa_timestamp 
                ON leituras (IdEtapa, Timestamp)");

            await _database.ExecuteAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_leitura_hash 
                ON leituras (Hash)");

            // Índices para FilaSincronizacao
            await _database.ExecuteAsync(@"
                CREATE INDEX IF NOT EXISTS idx_fila_status_prioridade 
                ON fila_sincronizacao (Status, Prioridade DESC, DataCriacao)");

            System.Diagnostics.Debug.WriteLine("[DB] Índices criados");
        }

        // ═══════════════════════════════════════════════════════════════════
        // OPERAÇÕES DE LEITURA
        // ═══════════════════════════════════════════════════════════════════

        public async Task<int> SalvarLeituraAsync(Leitura leitura)
        {
            var db = await GetDatabaseAsync();

            if (leitura.Id == 0)
            {
                leitura.DataCriacao = DateTime.UtcNow;
                await db.InsertAsync(leitura);
            }
            else
            {
                await db.UpdateAsync(leitura);
            }

            return leitura.Id;
        }

        public async Task<List<Leitura>> GetLeiturasAsync(int limite = 100)
        {
            var db = await GetDatabaseAsync();

            return await db.Table<Leitura>()
                .OrderByDescending(l => l.Timestamp)
                .Take(limite)
                .ToListAsync();
        }

        public async Task<List<Leitura>> GetLeiturasNaoSincronizadasAsync(int limite = 500)
        {
            var db = await GetDatabaseAsync();

            return await db.Table<Leitura>()
                .Where(l => !l.Sincronizado)
                .OrderBy(l => l.DataCriacao) // FIFO
                .Take(limite)
                .ToListAsync();
        }

        public async Task<int> ContarLeiturasAsync()
        {
            var db = await GetDatabaseAsync();
            return await db.Table<Leitura>().CountAsync();
        }

        public async Task<int> ContarLeiturasNaoSincronizadasAsync()
        {
            var db = await GetDatabaseAsync();
            return await db.Table<Leitura>()
                .Where(l => !l.Sincronizado)
                .CountAsync();
        }

        public async Task MarcarComoSincronizadaAsync(int id)
        {
            var db = await GetDatabaseAsync();

            await db.ExecuteAsync(@"
                UPDATE leituras 
                SET Sincronizado = 1, 
                    DataSincronizacao = ? 
                WHERE Id = ?",
                DateTime.UtcNow, id);
        }

        public async Task MarcarLoteSincronizadoAsync(IEnumerable<int> ids)
        {
            var db = await GetDatabaseAsync();

            var idList = string.Join(",", ids);

            await db.ExecuteAsync($@"
                UPDATE leituras 
                SET Sincronizado = 1, 
                    DataSincronizacao = '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}' 
                WHERE Id IN ({idList})");
        }

        public async Task<bool> ExisteLeituraComHashAsync(string hash)
        {
            var db = await GetDatabaseAsync();

            return await db.Table<Leitura>()
                .Where(l => l.Hash == hash)
                .CountAsync() > 0;
        }

        public async Task AtualizarTentativasSyncAsync(int id, int tentativas, string? erro)
        {
            var db = await GetDatabaseAsync();

            await db.ExecuteAsync(@"
                UPDATE leituras 
                SET TentativasSync = ?, 
                    ErroSync = ? 
                WHERE Id = ?",
                tentativas, erro, id);
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURAÇÕES
        // ═══════════════════════════════════════════════════════════════════

        public async Task<string?> GetConfigAsync(string chave)
        {
            var db = await GetDatabaseAsync();

            var config = await db.Table<Configuracao>()
                .Where(c => c.Chave == chave)
                .FirstOrDefaultAsync();

            return config?.Valor;
        }

        public async Task SetConfigAsync(string chave, string valor)
        {
            var db = await GetDatabaseAsync();

            var existing = await db.Table<Configuracao>()
                .Where(c => c.Chave == chave)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.Valor = valor;
                existing.DataAtualizacao = DateTime.UtcNow;
                await db.UpdateAsync(existing);
            }
            else
            {
                await db.InsertAsync(new Configuracao
                {
                    Chave = chave,
                    Valor = valor,
                    DataAtualizacao = DateTime.UtcNow
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // MANUTENÇÃO
        // ═══════════════════════════════════════════════════════════════════

        public async Task<long> ObterTamanhoBancoAsync()
        {
            var fileInfo = new FileInfo(DatabasePath);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }

        public async Task CompactarBancoAsync()
        {
            var db = await GetDatabaseAsync();
            await db.ExecuteAsync("VACUUM");

            System.Diagnostics.Debug.WriteLine("[DB] Banco compactado");
        }

        public async Task LimparLeiturasAntigasAsync(int diasManter = 30)
        {
            var db = await GetDatabaseAsync();

            var dataLimite = DateTime.UtcNow.AddDays(-diasManter);

            var deletadas = await db.ExecuteAsync(@"
                DELETE FROM leituras 
                WHERE Sincronizado = 1 
                AND DataCriacao < ?",
                dataLimite);

            System.Diagnostics.Debug.WriteLine($"[DB] {deletadas} leituras antigas removidas");
        }

        // ═══════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════

        public async ValueTask DisposeAsync()
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }

            _initLock.Dispose();
        }
    }
}