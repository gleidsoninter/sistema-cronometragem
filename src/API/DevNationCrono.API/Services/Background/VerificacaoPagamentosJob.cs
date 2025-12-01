using DevNationCrono.API.Services.Interfaces;

namespace DevNationCrono.API.Services.Background;

public class VerificacaoPagamentosJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VerificacaoPagamentosJob> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

    public VerificacaoPagamentosJob(
        IServiceProvider serviceProvider,
        ILogger<VerificacaoPagamentosJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job de verificação de pagamentos iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var pagamentoService = scope.ServiceProvider.GetRequiredService<IPagamentoService>();

                var count = await pagamentoService.VerificarCobrancasExpiradasAsync();

                if (count > 0)
                {
                    _logger.LogInformation("Verificação de pagamentos: {Count} cobranças atualizadas", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no job de verificação de pagamentos");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }
}
