using System.Text.RegularExpressions;
using AppColetor.Helpers;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class ParserService : IParserService
    {
        // ═══════════════════════════════════════════════════════════════════
        // REGEX PATTERNS
        // ═══════════════════════════════════════════════════════════════════

        // Formato GENÉRICO: NUMERO ou NUMERO,TIMESTAMP
        // Exemplos: "42" ou "42,2025-01-15 10:30:45.123"
        private static readonly Regex RegexGenerico = new(
            @"^(\d{1,4})(?:,(.+))?$",
            RegexOptions.Compiled);

        // Formato RF_TIMING: #MOTO:TEMPO:VOLTA# ou #MOTO:TEMPO#
        // Exemplos: "#042:10:30:45.123:001#" ou "#42:10:30:45#"
        private static readonly Regex RegexRfTiming = new(
            @"^#(\d{1,4}):([^:]+)(?::(\d{1,3}))?#$",
            RegexOptions.Compiled);

        // Formato AMB/MyLaps: @T:TRANS_ID:LOOP:TIME:HITS
        // Exemplo: "@T:1234567:01:103045123:003"
        private static readonly Regex RegexAmb = new(
            @"^@T:(\d+):(\d{1,2}):(\d{9}):(\d{1,3})$",
            RegexOptions.Compiled);

        // Formato apenas número (mais simples)
        private static readonly Regex RegexApenasNumero = new(
            @"^(\d{1,4})$",
            RegexOptions.Compiled);

        // ═══════════════════════════════════════════════════════════════════
        // PARSING
        // ═══════════════════════════════════════════════════════════════════

        public LeituraParseResult Parsear(string dados, string protocolo)
        {
            if (string.IsNullOrWhiteSpace(dados))
            {
                return LeituraParseResult.Falha("Dados vazios");
            }

            dados = dados.Trim();

            try
            {
                return protocolo switch
                {
                    Constants.PROTOCOLO_GENERICO => ParsearGenerico(dados),
                    Constants.PROTOCOLO_RF_TIMING => ParsearRfTiming(dados),
                    Constants.PROTOCOLO_AMB => ParsearAmb(dados),
                    _ => ParsearAutoDetect(dados)
                };
            }
            catch (Exception ex)
            {
                return LeituraParseResult.Falha($"Erro ao parsear: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta detectar automaticamente o formato dos dados
        /// </summary>
        private LeituraParseResult ParsearAutoDetect(string dados)
        {
            // Tentar cada formato
            if (dados.StartsWith("#") && dados.EndsWith("#"))
            {
                var result = ParsearRfTiming(dados);
                if (result.Sucesso) return result;
            }

            if (dados.StartsWith("@T:"))
            {
                var result = ParsearAmb(dados);
                if (result.Sucesso) return result;
            }

            // Por fim, tentar genérico
            return ParsearGenerico(dados);
        }

        /// <summary>
        /// Formato GENÉRICO: NUMERO ou NUMERO,TIMESTAMP
        /// </summary>
        private LeituraParseResult ParsearGenerico(string dados)
        {
            // Primeiro, tentar regex completo
            var match = RegexGenerico.Match(dados);

            if (!match.Success)
            {
                // Tentar apenas número
                var matchNumero = RegexApenasNumero.Match(dados);
                if (matchNumero.Success)
                {
                    if (!int.TryParse(matchNumero.Groups[1].Value, out int num))
                        return LeituraParseResult.Falha("Número inválido");

                    return LeituraParseResult.Ok(new Leitura
                    {
                        NumeroMoto = num,
                        Timestamp = DateTime.UtcNow,
                        Tipo = "P"
                    });
                }

                return LeituraParseResult.Falha("Formato não reconhecido");
            }

            // Parsear número da moto
            if (!int.TryParse(match.Groups[1].Value, out int numeroMoto))
            {
                return LeituraParseResult.Falha("Número da moto inválido");
            }

            // Parsear timestamp (se existir)
            var timestamp = DateTime.UtcNow;
            if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
            {
                var timestampStr = match.Groups[2].Value.Trim();

                // Tentar vários formatos de data/hora
                var formatos = new[]
                {
                    "yyyy-MM-dd HH:mm:ss.fff",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ss.fff",
                    "yyyy-MM-ddTHH:mm:ss",
                    "HH:mm:ss.fff",
                    "HH:mm:ss",
                    "dd/MM/yyyy HH:mm:ss.fff",
                    "dd/MM/yyyy HH:mm:ss"
                };

                bool parsed = false;
                foreach (var formato in formatos)
                {
                    if (DateTime.TryParseExact(timestampStr, formato,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var parsedTime))
                    {
                        // Se só tem hora, usar data atual
                        if (formato.StartsWith("HH"))
                        {
                            timestamp = DateTime.UtcNow.Date.Add(parsedTime.TimeOfDay);
                        }
                        else
                        {
                            timestamp = parsedTime.ToUniversalTime();
                        }
                        parsed = true;
                        break;
                    }
                }

                if (!parsed)
                {
                    // Não conseguiu parsear timestamp, usar atual
                    System.Diagnostics.Debug.WriteLine(
                        $"[Parser] Não foi possível parsear timestamp: {timestampStr}");
                }
            }

            return LeituraParseResult.Ok(new Leitura
            {
                NumeroMoto = numeroMoto,
                Timestamp = timestamp,
                Tipo = "P"
            });
        }

        /// <summary>
        /// Formato RF_TIMING: #MOTO:TEMPO:VOLTA#
        /// </summary>
        private LeituraParseResult ParsearRfTiming(string dados)
        {
            var match = RegexRfTiming.Match(dados);

            if (!match.Success)
            {
                return LeituraParseResult.Falha("Formato RF-TIMING inválido");
            }

            // Número da moto
            if (!int.TryParse(match.Groups[1].Value, out int numeroMoto))
            {
                return LeituraParseResult.Falha("Número da moto inválido");
            }

            // Timestamp
            var timestamp = DateTime.UtcNow;
            var tempoStr = match.Groups[2].Value;

            // Tentar parsear tempo (formato HH:mm:ss.fff ou HH:mm:ss)
            if (TimeSpan.TryParse(tempoStr, out var tempo))
            {
                timestamp = DateTime.UtcNow.Date.Add(tempo);
            }

            // Volta (opcional)
            int? volta = null;
            if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
            {
                if (int.TryParse(match.Groups[3].Value, out int v))
                {
                    volta = v;
                }
            }

            return LeituraParseResult.Ok(new Leitura
            {
                NumeroMoto = numeroMoto,
                Timestamp = timestamp,
                Tipo = "P",
                Volta = volta
            });
        }

        /// <summary>
        /// Formato AMB/MyLaps: @T:TRANS_ID:LOOP:TIME:HITS
        /// </summary>
        private LeituraParseResult ParsearAmb(string dados)
        {
            var match = RegexAmb.Match(dados);

            if (!match.Success)
            {
                return LeituraParseResult.Falha("Formato AMB inválido");
            }

            // Transponder ID
            if (!int.TryParse(match.Groups[1].Value, out int transponderId))
            {
                return LeituraParseResult.Falha("Transponder ID inválido");
            }

            // Loop/Antena
            if (!int.TryParse(match.Groups[2].Value, out int loop))
            {
                loop = 1;
            }

            // Timestamp (formato: HHMMSSMMM - 9 dígitos)
            var timestamp = DateTime.UtcNow;
            var timeStr = match.Groups[3].Value;

            if (timeStr.Length == 9)
            {
                try
                {
                    int horas = int.Parse(timeStr.Substring(0, 2));
                    int minutos = int.Parse(timeStr.Substring(2, 2));
                    int segundos = int.Parse(timeStr.Substring(4, 2));
                    int milissegundos = int.Parse(timeStr.Substring(6, 3));

                    var tempo = new TimeSpan(0, horas, minutos, segundos, milissegundos);
                    timestamp = DateTime.UtcNow.Date.Add(tempo);
                }
                catch
                {
                    // Usar timestamp atual se falhar
                }
            }

            // Hits (força do sinal)
            int hits = 0;
            if (match.Groups.Count > 4)
            {
                int.TryParse(match.Groups[4].Value, out hits);
            }

            // NOTA: Em um sistema real, o transponder ID precisaria ser
            // mapeado para o número da moto através de uma tabela de
            // cadastro. Por simplicidade, usamos o ID diretamente.

            return LeituraParseResult.Ok(new Leitura
            {
                NumeroMoto = transponderId % 10000, // Limitar a 4 dígitos
                Timestamp = timestamp,
                Tipo = loop == 1 ? "E" : "S", // Loop 1 = Entrada, Loop 2 = Saída
                DadosBrutos = dados
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // VALIDAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        public ValidationResult Validar(Leitura leitura)
        {
            var erros = new List<string>();

            // Validar número da moto
            if (leitura.NumeroMoto <= 0)
            {
                erros.Add("Número da moto deve ser maior que zero");
            }

            if (leitura.NumeroMoto > 9999)
            {
                erros.Add("Número da moto deve ter no máximo 4 dígitos");
            }

            // Validar timestamp
            if (leitura.Timestamp == default)
            {
                erros.Add("Timestamp é obrigatório");
            }

            // Verificar se timestamp não é muito antigo (mais de 24h)
            if (leitura.Timestamp < DateTime.UtcNow.AddHours(-24))
            {
                erros.Add("Timestamp muito antigo (mais de 24 horas)");
            }

            // Verificar se timestamp não é futuro
            if (leitura.Timestamp > DateTime.UtcNow.AddMinutes(5))
            {
                erros.Add("Timestamp no futuro");
            }

            // Validar tipo
            var tiposValidos = new[] { "P", "E", "S" };
            if (!tiposValidos.Contains(leitura.Tipo))
            {
                erros.Add($"Tipo inválido: {leitura.Tipo}. Tipos válidos: P, E, S");
            }

            // Validar ID da etapa
            if (leitura.IdEtapa <= 0)
            {
                erros.Add("ID da etapa é obrigatório");
            }

            // Validar volta (se informada)
            if (leitura.Volta.HasValue && leitura.Volta.Value < 0)
            {
                erros.Add("Volta não pode ser negativa");
            }

            if (erros.Count > 0)
            {
                return ValidationResult.Falha(erros.ToArray());
            }

            return ValidationResult.Ok();
        }
    }
}