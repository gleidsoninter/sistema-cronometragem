using AppColetor.Services.Interfaces;

namespace AppColetor.Helpers
{
    public static class TroubleshootingGuide
    {
        public static readonly List<TroubleshootingItem> Problemas = new()
        {
            // ═══════════════════════════════════════════════════════════════
            // PROBLEMAS DE USB
            // ═══════════════════════════════════════════════════════════════
            
            new TroubleshootingItem
            {
                Codigo = "USB_001",
                Titulo = "Dispositivo USB não detectado",
                Sintomas = new[]
                {
                    "Nenhum dispositivo aparece na lista",
                    "Mensagem 'Nenhum dispositivo encontrado'"
                },
                Causas = new[]
                {
                    "Cabo USB com defeito ou não suporta dados",
                    "Adaptador OTG não funcional",
                    "Driver do chip não suportado",
                    "Porta USB do celular com problema"
                },
                Solucoes = new[]
                {
                    "Verificar se o cabo é USB-OTG e suporta transferência de dados",
                    "Testar com outro cabo e/ou adaptador OTG",
                    "Verificar se o coletor está ligado e funcionando",
                    "Reiniciar o dispositivo Android",
                    "Verificar logs para identificar VID/PID do dispositivo"
                },
                ComoVerificar = @"
                    1. Vá em Configurações > Testes
                    2. Verifique a lista de dispositivos USB
                    3. Anote o VID/PID se aparecer
                    4. Compare com chips suportados (CH340, FTDI, CP210x, PL2303)"
            },

            new TroubleshootingItem
            {
                Codigo = "USB_002",
                Titulo = "Permissão USB negada",
                Sintomas = new[]
                {
                    "Pop-up de permissão aparece mas não funciona",
                    "Conexão falha após aprovar permissão"
                },
                Causas = new[]
                {
                    "Permissão negada anteriormente",
                    "Bug do Android com permissões USB"
                },
                Solucoes = new[]
                {
                    "Desconectar e reconectar o cabo USB",
                    "Reiniciar o app",
                    "Limpar dados do app nas configurações do Android",
                    "Reiniciar o dispositivo Android"
                }
            },

            new TroubleshootingItem
            {
                Codigo = "USB_003",
                Titulo = "Conexão USB cai frequentemente",
                Sintomas = new[]
                {
                    "Status alterna entre conectado e desconectado",
                    "Leituras param de chegar intermitentemente"
                },
                Causas = new[]
                {
                    "Cabo USB com mau contato",
                    "Interferência eletromagnética",
                    "Problema de energia no coletor",
                    "Baud rate incorreto"
                },
                Solucoes = new[]
                {
                    "Usar cabo USB de qualidade (curto e blindado)",
                    "Afastar de fontes de interferência",
                    "Verificar alimentação do coletor",
                    "Verificar configuração de baud rate (padrão: 115200)"
                }
            },
            
            // ═══════════════════════════════════════════════════════════════
            // PROBLEMAS DE SINCRONIZAÇÃO
            // ═══════════════════════════════════════════════════════════════
            
            new TroubleshootingItem
            {
                Codigo = "SYNC_001",
                Titulo = "Leituras não sincronizam",
                Sintomas = new[]
                {
                    "Contador de pendentes só aumenta",
                    "Indicador de sync sempre amarelo/vermelho"
                },
                Causas = new[]
                {
                    "Sem conexão com internet",
                    "URL da API incorreta",
                    "Token expirado",
                    "API fora do ar"
                },
                Solucoes = new[]
                {
                    "Verificar conexão WiFi/4G",
                    "Testar URL da API nas configurações",
                    "Reautenticar dispositivo",
                    "Verificar logs de erro",
                    "Contatar administrador da prova"
                }
            },

            new TroubleshootingItem
            {
                Codigo = "SYNC_002",
                Titulo = "Sync muito lento",
                Sintomas = new[]
                {
                    "Demora muito para sincronizar",
                    "Timeout frequente"
                },
                Causas = new[]
                {
                    "Conexão de internet lenta",
                    "Muitas leituras pendentes",
                    "API sobrecarregada"
                },
                Solucoes = new[]
                {
                    "Preferir WiFi em vez de 4G",
                    "Reduzir tamanho do lote de sync",
                    "Aumentar intervalo entre syncs",
                    "Verificar qualidade do sinal"
                }
            },
            
            // ═══════════════════════════════════════════════════════════════
            // PROBLEMAS DE BATERIA
            // ═══════════════════════════════════════════════════════════════
            
            new TroubleshootingItem
            {
                Codigo = "BAT_001",
                Titulo = "Bateria acaba muito rápido",
                Sintomas = new[]
                {
                    "Bateria não dura a prova toda",
                    "Aquecimento do dispositivo"
                },
                Causas = new[]
                {
                    "Tela sempre ligada",
                    "Sync muito frequente",
                    "Conexão 4G em área com sinal fraco"
                },
                Solucoes = new[]
                {
                    "Reduzir brilho da tela",
                    "Desativar 'Manter Tela Ligada' quando não necessário",
                    "Usar WiFi quando disponível",
                    "Desativar vibração",
                    "Ativar modo economia de bateria",
                    "Usar power bank"
                }
            },
            
            // ═══════════════════════════════════════════════════════════════
            // PROBLEMAS DE LEITURA
            // ═══════════════════════════════════════════════════════════════
            
            new TroubleshootingItem
            {
                Codigo = "READ_001",
                Titulo = "Leituras duplicadas",
                Sintomas = new[]
                {
                    "Mesma moto aparece várias vezes seguidas",
                    "Timestamps muito próximos"
                },
                Causas = new[]
                {
                    "Sensibilidade alta do coletor",
                    "Moto passando devagar",
                    "Transponder com problema"
                },
                Solucoes = new[]
                {
                    "O sistema já filtra duplicatas automaticamente",
                    "Ajustar sensibilidade do coletor (se possível)",
                    "Verificar posicionamento do coletor"
                }
            },

            new TroubleshootingItem
            {
                Codigo = "READ_002",
                Titulo = "Leituras não aparecem",
                Sintomas = new[]
                {
                    "Moto passa mas não registra",
                    "Contador de leituras não aumenta"
                },
                Causas = new[]
                {
                    "USB desconectado",
                    "Protocolo incorreto",
                    "Coletor não está enviando dados"
                },
                Solucoes = new[]
                {
                    "Verificar conexão USB (indicador verde)",
                    "Verificar protocolo configurado",
                    "Testar coletor com terminal serial",
                    "Verificar baud rate"
                }
            }
        };

        /// <summary>
        /// Busca solução por código ou sintoma
        /// </summary>
        public static List<TroubleshootingItem> Buscar(string termo)
        {
            var termoLower = termo.ToLower();

            return Problemas.Where(p =>
                p.Codigo.ToLower().Contains(termoLower) ||
                p.Titulo.ToLower().Contains(termoLower) ||
                p.Sintomas.Any(s => s.ToLower().Contains(termoLower)) ||
                p.Causas.Any(c => c.ToLower().Contains(termoLower))
            ).ToList();
        }

        /// <summary>
        /// Diagnóstico automático baseado em estado do app
        /// </summary>
        public static async Task<List<DiagnosticoResultado>> DiagnosticarAsync(
            IConnectivityService connectivity,
            ISerialService serial,
            IStorageService storage,
            IApiService api)
        {
            var resultados = new List<DiagnosticoResultado>();

            // Verificar USB
            if (!serial.IsConnected)
            {
                resultados.Add(new DiagnosticoResultado
                {
                    Area = "USB",
                    Status = DiagnosticoStatus.Erro,
                    Mensagem = "USB não conectado",
                    CodigoProblema = "USB_001"
                });
            }
            else
            {
                resultados.Add(new DiagnosticoResultado
                {
                    Area = "USB",
                    Status = DiagnosticoStatus.Ok,
                    Mensagem = $"Conectado: {serial.DeviceName}"
                });
            }

            // Verificar conectividade
            if (!connectivity.IsOnline)
            {
                resultados.Add(new DiagnosticoResultado
                {
                    Area = "Internet",
                    Status = DiagnosticoStatus.Erro,
                    Mensagem = "Sem conexão com internet",
                    CodigoProblema = "SYNC_001"
                });
            }
            else
            {
                resultados.Add(new DiagnosticoResultado
                {
                    Area = "Internet",
                    Status = DiagnosticoStatus.Ok,
                    Mensagem = $"Conectado via {connectivity.TipoAtual}"
                });
            }

            // Verificar API
            var apiOk = await api.VerificarConexaoAsync();
            resultados.Add(new DiagnosticoResultado
            {
                Area = "API",
                Status = apiOk ? DiagnosticoStatus.Ok : DiagnosticoStatus.Erro,
                Mensagem = apiOk ? "API acessível" : "API inacessível",
                CodigoProblema = apiOk ? null : "SYNC_001"
            });

            // Verificar pendentes
            var pendentes = await storage.ContarLeiturasNaoSincronizadasAsync();
            if (pendentes > 100)
            {
                resultados.Add(new DiagnosticoResultado
                {
                    Area = "Sincronização",
                    Status = DiagnosticoStatus.Alerta,
                    Mensagem = $"{pendentes} leituras pendentes",
                    CodigoProblema = "SYNC_002"
                });
            }
            else
            {
                resultados.Add(new DiagnosticoResultado
                {
                    Area = "Sincronização",
                    Status = DiagnosticoStatus.Ok,
                    Mensagem = $"{pendentes} pendentes"
                });
            }

            // Verificar bateria
            try
            {
                var bateria = Battery.Default.ChargeLevel;
                if (bateria < 0.2)
                {
                    resultados.Add(new DiagnosticoResultado
                    {
                        Area = "Bateria",
                        Status = DiagnosticoStatus.Alerta,
                        Mensagem = $"Bateria baixa: {bateria * 100:F0}%",
                        CodigoProblema = "BAT_001"
                    });
                }
                else
                {
                    resultados.Add(new DiagnosticoResultado
                    {
                        Area = "Bateria",
                        Status = DiagnosticoStatus.Ok,
                        Mensagem = $"{bateria * 100:F0}%"
                    });
                }
            }
            catch { }

            return resultados;
        }
    }

    public class TroubleshootingItem
    {
        public string Codigo { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string[] Sintomas { get; set; } = Array.Empty<string>();
        public string[] Causas { get; set; } = Array.Empty<string>();
        public string[] Solucoes { get; set; } = Array.Empty<string>();
        public string? ComoVerificar { get; set; }
    }

    public class DiagnosticoResultado
    {
        public string Area { get; set; } = "";
        public DiagnosticoStatus Status { get; set; }
        public string Mensagem { get; set; } = "";
        public string? CodigoProblema { get; set; }
    }

    public enum DiagnosticoStatus
    {
        Ok,
        Alerta,
        Erro
    }
}