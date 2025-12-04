using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class ConflictResolver
    {
        private readonly IStorageService _storageService;

        public ConflictResolver(IStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Verifica se uma leitura é duplicata
        /// </summary>
        public async Task<DuplicataCheckResult> VerificarDuplicataAsync(Leitura novaLeitura)
        {
            var resultado = new DuplicataCheckResult { IsDuplicata = false };

            // Verificar por hash (mais confiável)
            if (!string.IsNullOrEmpty(novaLeitura.Hash))
            {
                var existePorHash = await _storageService.ExisteLeituraAsync(novaLeitura.Hash);

                if (existePorHash)
                {
                    resultado.IsDuplicata = true;
                    resultado.Motivo = "Hash idêntico encontrado";
                    resultado.TipoDuplicata = TipoDuplicata.HashIdentico;
                    return resultado;
                }
            }

            // Verificar por proximidade temporal (mesma moto em intervalo curto)
            var leiturasRecentes = await _storageService.GetLeiturasRecentesAsync(100);

            var leituraSimilar = leiturasRecentes.FirstOrDefault(l =>
                l.NumeroMoto == novaLeitura.NumeroMoto &&
                l.IdEtapa == novaLeitura.IdEtapa &&
                Math.Abs((l.Timestamp - novaLeitura.Timestamp).TotalSeconds) < 2);

            if (leituraSimilar != null)
            {
                resultado.IsDuplicata = true;
                resultado.Motivo = $"Leitura similar há {Math.Abs((leituraSimilar.Timestamp - novaLeitura.Timestamp).TotalMilliseconds):F0}ms";
                resultado.TipoDuplicata = TipoDuplicata.ProximidadeTemporal;
                resultado.LeituraExistente = leituraSimilar;
                return resultado;
            }

            return resultado;
        }

        /// <summary>
        /// Resolve conflito entre leitura local e resposta da API
        /// </summary>
        public ConflictResolution ResolverConflito(Leitura local, LeituraConflito servidor)
        {
            var resolucao = new ConflictResolution
            {
                IdLeituraLocal = local.Id,
                Estrategia = EstrategiaResolucao.ManterLocal
            };

            // Se servidor retornou duplicata, marcar como sincronizada
            if (servidor.Status == "DUPLICADA")
            {
                resolucao.Estrategia = EstrategiaResolucao.MarcarSincronizada;
                resolucao.Mensagem = "Leitura já existe no servidor";
                return resolucao;
            }

            // Se servidor retornou erro de validação, manter local para retry
            if (servidor.Status == "ERRO_VALIDACAO")
            {
                resolucao.Estrategia = EstrategiaResolucao.ManterLocal;
                resolucao.Mensagem = servidor.Mensagem;
                return resolucao;
            }

            // Se servidor processou com sucesso
            if (servidor.Status == "OK")
            {
                resolucao.Estrategia = EstrategiaResolucao.MarcarSincronizada;
                resolucao.Mensagem = "Sincronizado com sucesso";
                return resolucao;
            }

            // Caso padrão: manter local
            return resolucao;
        }

        /// <summary>
        /// Limpa duplicatas locais
        /// </summary>
        public async Task<int> LimparDuplicatasLocaisAsync()
        {
            var leituras = await _storageService.GetLeiturasRecentesAsync(10000);
            var duplicatas = new List<int>();
            var hashesVistos = new HashSet<string>();

            foreach (var leitura in leituras.OrderBy(l => l.DataCriacao))
            {
                if (!string.IsNullOrEmpty(leitura.Hash))
                {
                    if (hashesVistos.Contains(leitura.Hash))
                    {
                        duplicatas.Add(leitura.Id);
                    }
                    else
                    {
                        hashesVistos.Add(leitura.Hash);
                    }
                }
            }

            // Remover duplicatas (manter a mais antiga)
            if (duplicatas.Count > 0)
            {
                // TODO: Implementar remoção no StorageService
                System.Diagnostics.Debug.WriteLine(
                    $"[Conflict] {duplicatas.Count} duplicatas identificadas para remoção");
            }

            return duplicatas.Count;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLASSES AUXILIARES
    // ═══════════════════════════════════════════════════════════════════════

    public class DuplicataCheckResult
    {
        public bool IsDuplicata { get; set; }
        public string? Motivo { get; set; }
        public TipoDuplicata TipoDuplicata { get; set; }
        public Leitura? LeituraExistente { get; set; }
    }

    public enum TipoDuplicata
    {
        Nenhuma,
        HashIdentico,
        ProximidadeTemporal,
        MesmaVolta
    }

    public class LeituraConflito
    {
        public string Status { get; set; } = "";
        public string? Mensagem { get; set; }
        public int? IdServidor { get; set; }
        public DateTime? TimestampServidor { get; set; }
    }

    public class ConflictResolution
    {
        public int IdLeituraLocal { get; set; }
        public EstrategiaResolucao Estrategia { get; set; }
        public string? Mensagem { get; set; }
    }

    public enum EstrategiaResolucao
    {
        ManterLocal,
        UsarServidor,
        MarcarSincronizada,
        Descartar
    }
}