using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppColetor.Helpers;
using AppColetor.Models.DTOs;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class ApiService : IApiService, IDisposable
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════════════════════════════

        private const int TIMEOUT_SECONDS = 30;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int INITIAL_RETRY_DELAY_MS = 1000;
        private const int MAX_RETRY_DELAY_MS = 16000;
        private const int LOTE_MAX_SIZE = 50;

        // ═══════════════════════════════════════════════════════════════════
        // CAMPOS PRIVADOS
        // ═══════════════════════════════════════════════════════════════════

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private string _baseUrl = "";
        private string _token = "";
        private DateTime _tokenExpiration = DateTime.MinValue;
        private bool _isConnected;
        private bool _disposed;

        // ═══════════════════════════════════════════════════════════════════
        // EVENTOS
        // ═══════════════════════════════════════════════════════════════════

        public event EventHandler<ApiStatusEventArgs>? StatusChanged;

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES
        // ═══════════════════════════════════════════════════════════════════

        public bool IsConnected => _isConnected;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_token) && _tokenExpiration > DateTime.UtcNow;

        // ═══════════════════════════════════════════════════════════════════
        // CONSTRUTOR
        // ═══════════════════════════════════════════════════════════════════

        public ApiService()
        {
            // Configurar handler com pooling de conexões
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 10,
                EnableMultipleHttp2Connections = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS)
            };

            // Headers padrão
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AppColetor/1.0");

            // Opções de serialização JSON
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

            // Monitorar conectividade
            Connectivity.ConnectivityChanged += OnConnectivityChanged;
        }


        // Adicionar ao IApiService:
        Task<RegistroDispositivoResultDto?> RegistrarDispositivoAsync(
            DispositivoColetor dispositivo,
            string senha,
            CancellationToken cancellationToken = default);

        // Implementação no ApiService:
        public async Task<RegistroDispositivoResultDto?> RegistrarDispositivoAsync(
            DispositivoColetor dispositivo,
            string senha,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
                return null;

            try
            {
                var dto = new
                {
                    deviceId = dispositivo.DeviceId,
                    nome = dispositivo.Nome,
                    tipo = dispositivo.Tipo,
                    idEspecial = dispositivo.IdEspecial,
                    idEtapa = dispositivo.IdEtapa,
                    androidId = dispositivo.AndroidId,
                    modeloDispositivo = dispositivo.ModeloDispositivo,
                    fabricante = dispositivo.Fabricante,
                    versaoApp = dispositivo.VersaoApp,
                    senha = senha
                };

                var url = $"{_baseUrl}/api/v1/dispositivos/registrar";

                var response = await ExecuteWithRetryAsync(async (ct) =>
                {
                    return await _httpClient.PostAsJsonAsync(url, dto, _jsonOptions, ct);
                }, cancellationToken);

                if (response?.IsSuccessStatusCode == true)
                {
                    var resultado = await response.Content.ReadFromJsonAsync<RegistroDispositivoResultDto>(
                        _jsonOptions, cancellationToken);

                    if (resultado?.Sucesso == true && !string.IsNullOrEmpty(resultado.Token))
                    {
                        // Salvar token
                        _token = resultado.Token;
                        _tokenExpiration = resultado.ExpiraEm ?? DateTime.UtcNow.AddHours(24);

                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", _token);

                        System.Diagnostics.Debug.WriteLine("[API] Dispositivo registrado com sucesso");
                    }

                    return resultado;
                }

                var error = await TryReadErrorAsync(response!, cancellationToken);
                return new RegistroDispositivoResultDto
                {
                    Sucesso = false,
                    Mensagem = error?.Mensagem ?? "Erro ao registrar dispositivo"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro ao registrar dispositivo: {ex.Message}");
                return new RegistroDispositivoResultDto
                {
                    Sucesso = false,
                    Mensagem = ex.Message
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        public void ConfigurarUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return;

            // Garantir que URL termina sem barra
            _baseUrl = baseUrl.TrimEnd('/');

            // Atualizar BaseAddress do HttpClient
            try
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] URL inválida: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[API] URL configurada: {_baseUrl}");
        }

        public void ConfigurarToken(string token)
        {
            _token = token ?? "";

            // Atualizar header de autorização
            _httpClient.DefaultRequestHeaders.Authorization =
                string.IsNullOrEmpty(_token)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", _token);

            System.Diagnostics.Debug.WriteLine(
                $"[API] Token configurado: {(string.IsNullOrEmpty(_token) ? "vazio" : "***")}");
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            var connected = e.NetworkAccess == NetworkAccess.Internet;

            if (_isConnected != connected)
            {
                _isConnected = connected;

                RaiseStatusChanged(connected, IsAuthenticated,
                    connected ? "Conexão de rede disponível" : "Sem conexão de rede");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // VERIFICAÇÃO DE CONEXÃO
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> VerificarConexaoAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                _isConnected = false;
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[API] Verificando conexão...");

                // Usar endpoint de health check ou versão
                var url = $"{_baseUrl}/api/v1/health";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // Timeout curto para verificação

                var response = await _httpClient.SendAsync(request, cts.Token);

                _isConnected = response.IsSuccessStatusCode;

                System.Diagnostics.Debug.WriteLine(
                    $"[API] Conexão: {(_isConnected ? "OK" : "FALHA")} - Status: {response.StatusCode}");

                RaiseStatusChanged(_isConnected, IsAuthenticated,
                    _isConnected ? "API acessível" : $"API inacessível ({response.StatusCode})");

                return _isConnected;
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[API] Timeout na verificação de conexão");
                _isConnected = false;
                RaiseStatusChanged(false, false, "Timeout");
                return false;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro de conexão: {ex.Message}");
                _isConnected = false;
                RaiseStatusChanged(false, false, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro inesperado: {ex.Message}");
                _isConnected = false;
                RaiseStatusChanged(false, false, ex.Message);
                return false;
            }
        }

        private void RaiseStatusChanged(bool connected, bool authenticated, string? message = null,
            int? statusCode = null)
        {
            StatusChanged?.Invoke(this, new ApiStatusEventArgs
            {
                IsConnected = connected,
                IsAuthenticated = authenticated,
                Message = message,
                StatusCode = statusCode
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // AUTENTICAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        public async Task<AuthResultDto?> AutenticarDispositivoAsync(
            string deviceId,
            string senha,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                System.Diagnostics.Debug.WriteLine("[API] URL não configurada");
                return null;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[API] Autenticando dispositivo: {deviceId}");

                var request = new AuthRequestDto
                {
                    DeviceId = deviceId,
                    Senha = senha,
                    Tipo = "COLETOR"
                };

                var url = $"{_baseUrl}/api/v1/auth/dispositivo";

                var response = await ExecuteWithRetryAsync(
                    async (ct) => { return await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, ct); },
                    cancellationToken);

                if (response == null)
                    return null;

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<AuthResultDto>(_jsonOptions, cancellationToken);

                    if (result?.Sucesso == true && !string.IsNullOrEmpty(result.Token))
                    {
                        // Salvar token
                        _token = result.Token;
                        _tokenExpiration = result.ExpiraEm ?? DateTime.UtcNow.AddHours(24);

                        // Configurar header
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", _token);

                        System.Diagnostics.Debug.WriteLine(
                            $"[API] Autenticado com sucesso. Token expira em: {_tokenExpiration}");

                        RaiseStatusChanged(true, true, "Autenticado");
                    }

                    return result;
                }
                else
                {
                    var error = await TryReadErrorAsync(response, cancellationToken);

                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Falha na autenticação: {response.StatusCode} - {error?.Mensagem}");

                    return new AuthResultDto
                    {
                        Sucesso = false,
                        Mensagem = error?.Mensagem ?? $"Erro {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro na autenticação: {ex.Message}");

                return new AuthResultDto
                {
                    Sucesso = false,
                    Mensagem = $"Erro de conexão: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verifica se o token ainda é válido e renova se necessário
        /// </summary>
        private async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken)
        {
            // Se não tem token, não está autenticado
            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[API] Sem token configurado");
                return false;
            }

            // Verificar se token está prestes a expirar (margem de 5 minutos)
            if (_tokenExpiration <= DateTime.UtcNow.AddMinutes(5))
            {
                System.Diagnostics.Debug.WriteLine("[API] Token expirado ou expirando, precisa reautenticar");

                // Tentar renovar token
                var deviceId = await SecureStorage.GetAsync(Constants.KEY_DEVICE_ID);
                var senha = await SecureStorage.GetAsync("device_senha");

                if (!string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(senha))
                {
                    var result = await AutenticarDispositivoAsync(deviceId, senha, cancellationToken);
                    return result?.Sucesso == true;
                }

                return false;
            }

            return true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENVIO DE LEITURAS
        // ═══════════════════════════════════════════════════════════════════

        public async Task<LeituraResponseDto?> EnviarLeituraAsync(
            Leitura leitura,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                System.Diagnostics.Debug.WriteLine("[API] URL não configurada");
                return null;
            }

            // Verificar autenticação
            if (!await EnsureAuthenticatedAsync(cancellationToken))
            {
                System.Diagnostics.Debug.WriteLine("[API] Não autenticado");
                return null;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[API] Enviando leitura: #{leitura.NumeroMoto}");

                // Converter para DTO
                var dto = new LeituraDto
                {
                    NumeroMoto = leitura.NumeroMoto,
                    Timestamp = leitura.Timestamp,
                    Tipo = leitura.Tipo,
                    IdEtapa = leitura.IdEtapa,
                    Volta = leitura.Volta,
                    IdEspecial = leitura.IdEspecial,
                    DeviceId = leitura.DeviceId,
                    Hash = leitura.Hash
                };

                var url = $"{_baseUrl}/api/v1/leituras";

                var response = await ExecuteWithRetryAsync(async (ct) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = JsonContent.Create(dto, mediaType: null, _jsonOptions)
                    };

                    // Adicionar headers de rastreamento
                    request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
                    request.Headers.Add("X-Device-ID", leitura.DeviceId);

                    return await _httpClient.SendAsync(request, ct);
                }, cancellationToken);

                if (response == null)
                    return null;

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LeituraResponseDto>(_jsonOptions, cancellationToken);

                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Leitura enviada: #{leitura.NumeroMoto} - Status: {result?.Status}");

                    return result;
                }
                else
                {
                    var error = await TryReadErrorAsync(response, cancellationToken);

                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Erro ao enviar leitura: {response.StatusCode} - {error?.Mensagem}");

                    // Retornar resposta de erro
                    return new LeituraResponseDto
                    {
                        Status = "ERRO",
                        Mensagem = error?.Mensagem ?? $"Erro HTTP {(int)response.StatusCode}",
                        NumeroMoto = leitura.NumeroMoto
                    };
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[API] Envio cancelado");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro ao enviar leitura: {ex.Message}");

                return new LeituraResponseDto
                {
                    Status = "ERRO",
                    Mensagem = ex.Message,
                    NumeroMoto = leitura.NumeroMoto
                };
            }
        }

        public async Task<LoteResponseDto?> EnviarLoteAsync(
                    IEnumerable<Leitura> leituras,
                    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                System.Diagnostics.Debug.WriteLine("[API] URL não configurada");
                return null;
            }

            // Verificar autenticação
            if (!await EnsureAuthenticatedAsync(cancellationToken))
            {
                System.Diagnostics.Debug.WriteLine("[API] Não autenticado");
                return null;
            }

            var listaLeituras = leituras.ToList();

            if (listaLeituras.Count == 0)
            {
                return new LoteResponseDto
                {
                    TotalRecebidas = 0,
                    TotalProcessadas = 0,
                    TotalDuplicadas = 0,
                    TotalErros = 0
                };
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[API] Enviando lote: {listaLeituras.Count} leituras");

                // Obter device ID comum
                var deviceId = listaLeituras.First().DeviceId;
                var idEtapa = listaLeituras.First().IdEtapa;

                // Converter para DTO
                var loteDto = new LoteLeituraDto
                {
                    DeviceId = deviceId,
                    IdEtapa = idEtapa,
                    Leituras = listaLeituras.Select(l => new LeituraItemDto
                    {
                        NumeroMoto = l.NumeroMoto,
                        Timestamp = l.Timestamp,
                        Tipo = l.Tipo,
                        Volta = l.Volta,
                        IdEspecial = l.IdEspecial,
                        Hash = l.Hash
                    }).ToList()
                };

                var url = $"{_baseUrl}/api/v1/leituras/lote";

                var response = await ExecuteWithRetryAsync(async (ct) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = JsonContent.Create(loteDto, mediaType: null, _jsonOptions)
                    };

                    request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
                    request.Headers.Add("X-Device-ID", deviceId);
                    request.Headers.Add("X-Lote-Size", listaLeituras.Count.ToString());

                    return await _httpClient.SendAsync(request, ct);
                }, cancellationToken);

                if (response == null)
                    return null;

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoteResponseDto>(_jsonOptions, cancellationToken);

                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Lote enviado: Recebidas={result?.TotalRecebidas}, " +
                        $"Processadas={result?.TotalProcessadas}, " +
                        $"Duplicadas={result?.TotalDuplicadas}, " +
                        $"Erros={result?.TotalErros}");

                    return result;
                }
                else
                {
                    var error = await TryReadErrorAsync(response, cancellationToken);

                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Erro ao enviar lote: {response.StatusCode} - {error?.Mensagem}");

                    return new LoteResponseDto
                    {
                        TotalRecebidas = listaLeituras.Count,
                        TotalProcessadas = 0,
                        TotalDuplicadas = 0,
                        TotalErros = listaLeituras.Count
                    };
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[API] Envio de lote cancelado");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro ao enviar lote: {ex.Message}");

                return new LoteResponseDto
                {
                    TotalRecebidas = listaLeituras.Count,
                    TotalProcessadas = 0,
                    TotalDuplicadas = 0,
                    TotalErros = listaLeituras.Count
                };
            }
        }

        public async Task<List<Leitura>> SincronizarLeiturasAsync(
            IEnumerable<Leitura> leituras,
            CancellationToken cancellationToken = default)
        {
            var sincronizadas = new List<Leitura>();
            var pendentes = leituras.ToList();

            if (pendentes.Count == 0)
                return sincronizadas;

            System.Diagnostics.Debug.WriteLine($"[API] Sincronizando {pendentes.Count} leituras...");

            // Dividir em lotes
            var lotes = DividirEmLotes(pendentes, LOTE_MAX_SIZE);
            var loteAtual = 0;
            var totalLotes = lotes.Count;

            foreach (var lote in lotes)
            {
                loteAtual++;

                if (cancellationToken.IsCancellationRequested)
                    break;

                System.Diagnostics.Debug.WriteLine($"[API] Enviando lote {loteAtual}/{totalLotes}...");

                var response = await EnviarLoteAsync(lote, cancellationToken);

                if (response == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[API] Falha no lote {loteAtual}, interrompendo sincronização");
                    break;
                }

                // Se o lote foi processado (mesmo com duplicadas), marcar como sincronizado
                if (response.TotalProcessadas > 0 || response.TotalDuplicadas > 0)
                {
                    // Marcar todas do lote como sincronizadas
                    // (duplicadas também são consideradas OK pois já existem no servidor)
                    sincronizadas.AddRange(lote);
                }
                else if (response.TotalErros == lote.Count)
                {
                    // Se todas deram erro, verificar detalhes
                    if (response.Detalhes != null)
                    {
                        foreach (var detalhe in response.Detalhes)
                        {
                            if (detalhe.Status == "OK" || detalhe.Status == "DUPLICADA")
                            {
                                var leitura = lote.FirstOrDefault(l => l.NumeroMoto == detalhe.NumeroMoto);
                                if (leitura != null)
                                    sincronizadas.Add(leitura);
                            }
                        }
                    }
                }

                // Pequeno delay entre lotes para não sobrecarregar a API
                if (loteAtual < totalLotes)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[API] Sincronização concluída: {sincronizadas.Count}/{pendentes.Count} leituras");

            return sincronizadas;
        }

        private List<List<T>> DividirEmLotes<T>(List<T> lista, int tamanhoLote)
        {
            var lotes = new List<List<T>>();

            for (int i = 0; i < lista.Count; i += tamanhoLote)
            {
                lotes.Add(lista.GetRange(i, Math.Min(tamanhoLote, lista.Count - i)));
            }

            return lotes;
        }

        /// <summary>
        /// Executa uma requisição HTTP com retry e backoff exponencial
        /// </summary>
        private async Task<HttpResponseMessage?> ExecuteWithRetryAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> action,
            CancellationToken cancellationToken,
            int? maxRetries = null)
        {
            var tentativas = maxRetries ?? MAX_RETRY_ATTEMPTS;
            var delayMs = INITIAL_RETRY_DELAY_MS;

            for (int tentativa = 1; tentativa <= tentativas; tentativa++)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        return null;

                    var response = await action(cancellationToken);

                    // Verificar se deve fazer retry baseado no status code
                    if (ShouldRetry(response.StatusCode, tentativa, tentativas))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[API] Tentativa {tentativa}/{tentativas} falhou com {response.StatusCode}. " +
                            $"Retry em {delayMs}ms...");

                        await Task.Delay(delayMs, cancellationToken);

                        // Backoff exponencial com jitter
                        delayMs = CalcularProximoDelay(delayMs);

                        continue;
                    }

                    return response;
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout - tentar novamente
                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Timeout na tentativa {tentativa}/{tentativas}. Retry em {delayMs}ms...");

                    if (tentativa < tentativas)
                    {
                        await Task.Delay(delayMs, cancellationToken);
                        delayMs = CalcularProximoDelay(delayMs);
                    }
                }
                catch (HttpRequestException ex) when (IsTransientError(ex))
                {
                    // Erro transiente - tentar novamente
                    System.Diagnostics.Debug.WriteLine(
                        $"[API] Erro transiente na tentativa {tentativa}/{tentativas}: {ex.Message}. " +
                        $"Retry em {delayMs}ms...");

                    if (tentativa < tentativas)
                    {
                        await Task.Delay(delayMs, cancellationToken);
                        delayMs = CalcularProximoDelay(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    // Erro não recuperável
                    System.Diagnostics.Debug.WriteLine($"[API] Erro não recuperável: {ex.Message}");
                    throw;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[API] Todas as {tentativas} tentativas falharam");
            return null;
        }

        /// <summary>
        /// Verifica se deve fazer retry baseado no status code
        /// </summary>
        private bool ShouldRetry(HttpStatusCode statusCode, int tentativaAtual, int maxTentativas)
        {
            if (tentativaAtual >= maxTentativas)
                return false;

            // Status codes que justificam retry
            return statusCode switch
            {
                HttpStatusCode.RequestTimeout => true,           // 408
                HttpStatusCode.TooManyRequests => true,          // 429
                HttpStatusCode.InternalServerError => true,      // 500
                HttpStatusCode.BadGateway => true,               // 502
                HttpStatusCode.ServiceUnavailable => true,       // 503
                HttpStatusCode.GatewayTimeout => true,           // 504
                _ => false
            };
        }

        /// <summary>
        /// Verifica se é um erro transiente que justifica retry
        /// </summary>
        private bool IsTransientError(HttpRequestException ex)
        {
            // Erros de conexão geralmente são transientes
            if (ex.InnerException is System.Net.Sockets.SocketException)
                return true;

            // Verificar status code se disponível
            if (ex.StatusCode.HasValue)
            {
                return ShouldRetry(ex.StatusCode.Value, 1, 2);
            }

            // Verificar mensagem de erro
            var message = ex.Message.ToLower();
            return message.Contains("connection") ||
                   message.Contains("timeout") ||
                   message.Contains("network") ||
                   message.Contains("refused");
        }

        /// <summary>
        /// Calcula o próximo delay com backoff exponencial e jitter
        /// </summary>
        private int CalcularProximoDelay(int delayAtualMs)
        {
            // Backoff exponencial: delay * 2
            var novoDelay = delayAtualMs * 2;

            // Aplicar limite máximo
            novoDelay = Math.Min(novoDelay, MAX_RETRY_DELAY_MS);

            // Adicionar jitter (variação aleatória de ±25%)
            var jitter = novoDelay * 0.25;
            var random = new Random();
            novoDelay += (int)(random.NextDouble() * jitter * 2 - jitter);

            return Math.Max(INITIAL_RETRY_DELAY_MS, novoDelay);
        }

        // ═══════════════════════════════════════════════════════════════════
        // MÉTODOS AUXILIARES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém informações da etapa
        /// </summary>
        public async Task<EtapaInfoDto?> ObterEtapaAsync(int idEtapa, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
                return null;

            try
            {
                var url = $"{_baseUrl}/api/v1/etapas/{idEtapa}";

                var response = await ExecuteWithRetryAsync(async (ct) =>
                {
                    return await _httpClient.GetAsync(url, ct);
                }, cancellationToken, maxRetries: 2);

                if (response?.IsSuccessStatusCode == true)
                {
                    return await response.Content.ReadFromJsonAsync<EtapaInfoDto>(_jsonOptions, cancellationToken);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro ao obter etapa: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Envia heartbeat para indicar que o coletor está ativo
        /// </summary>
        public async Task<bool> EnviarHeartbeatAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_baseUrl))
                return false;

            try
            {
                var dto = new HeartbeatDto
                {
                    DeviceId = deviceId,
                    Timestamp = DateTime.UtcNow,
                    Status = "ATIVO"
                };

                // Tentar obter nível da bateria
                try
                {
                    dto.Bateria = (int)(Battery.Default.ChargeLevel * 100);
                }
                catch { }

                var url = $"{_baseUrl}/api/v1/dispositivos/heartbeat";

                // Usar timeout curto para heartbeat
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await _httpClient.PostAsJsonAsync(url, dto, _jsonOptions, cts.Token);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Erro no heartbeat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta ler erro da resposta HTTP
        /// </summary>
        private async Task<ApiErrorDto?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!string.IsNullOrEmpty(content))
                {
                    // Tentar deserializar como ApiErrorDto
                    try
                    {
                        return JsonSerializer.Deserialize<ApiErrorDto>(content, _jsonOptions);
                    }
                    catch
                    {
                        // Se não conseguir deserializar, criar erro genérico
                        return new ApiErrorDto
                        {
                            Codigo = response.StatusCode.ToString(),
                            Mensagem = content.Length > 200 ? content.Substring(0, 200) : content
                        };
                    }
                }

                return new ApiErrorDto
                {
                    Codigo = response.StatusCode.ToString(),
                    Mensagem = response.ReasonPhrase ?? "Erro desconhecido"
                };
            }
            catch
            {
                return new ApiErrorDto
                {
                    Codigo = response.StatusCode.ToString(),
                    Mensagem = "Não foi possível ler detalhes do erro"
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // DISPOSE
        // ═══════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Connectivity.ConnectivityChanged -= OnConnectivityChanged;
            _httpClient.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}