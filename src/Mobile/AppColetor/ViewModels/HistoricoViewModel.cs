using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AppColetor.ViewModels
{
    public partial class HistoricoViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        private readonly IApiService _apiService;

        public HistoricoViewModel(IStorageService storageService, IApiService apiService)
        {
            _storageService = storageService;
            _apiService = apiService;
            Title = "Histórico";
        }

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES
        // ═══════════════════════════════════════════════════════════════════

        public ObservableCollection<Leitura> Leituras { get; } = new();

        [ObservableProperty]
        private int _totalLeituras;

        [ObservableProperty]
        private int _leiturasSincronizadas;

        [ObservableProperty]
        private int _leiturasPendentes;

        [ObservableProperty]
        private string _filtro = "TODAS";

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private Leitura? _leituraSelecionada;

        // ═══════════════════════════════════════════════════════════════════
        // COMANDOS
        // ═══════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task CarregarAsync()
        {
            try
            {
                IsRefreshing = true;

                // Carregar leituras do banco
                var leituras = await _storageService.GetLeiturasRecentesAsync(500);

                // Aplicar filtro
                var filtradas = Filtro switch
                {
                    "PENDENTES" => leituras.Where(l => !l.Sincronizado).ToList(),
                    "SINCRONIZADAS" => leituras.Where(l => l.Sincronizado).ToList(),
                    _ => leituras
                };

                Leituras.Clear();
                foreach (var leitura in filtradas)
                {
                    Leituras.Add(leitura);
                }

                // Atualizar estatísticas
                TotalLeituras = leituras.Count;
                LeiturasSincronizadas = leituras.Count(l => l.Sincronizado);
                LeiturasPendentes = leituras.Count(l => !l.Sincronizado);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao carregar histórico: {ex.Message}", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task FiltrarAsync(string filtro)
        {
            Filtro = filtro;
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task SincronizarPendentesAsync()
        {
            await ExecuteAsync(async () =>
            {
                var pendentes = await _storageService.GetLeiturasNaoSincronizadasAsync();

                if (pendentes.Count == 0)
                {
                    await Shell.Current.DisplayAlert("Info", "Nenhuma leitura pendente", "OK");
                    return;
                }

                var confirmacao = await Shell.Current.DisplayAlert(
                    "Sincronizar",
                    $"Deseja sincronizar {pendentes.Count} leituras pendentes?",
                    "Sim", "Não");

                if (!confirmacao) return;

                StatusMessage = $"Sincronizando {pendentes.Count} leituras...";

                var sincronizadas = await _apiService.SincronizarLeiturasAsync(pendentes);

                foreach (var leitura in sincronizadas)
                {
                    await _storageService.MarcarComoSincronizadaAsync(leitura.Id);
                }

                await CarregarAsync();

                await Shell.Current.DisplayAlert(
                    "Concluído",
                    $"{sincronizadas.Count} de {pendentes.Count} leituras sincronizadas",
                    "OK");

            }, "Sincronizando...");
        }

        [RelayCommand]
        private async Task VerDetalhesAsync(Leitura leitura)
        {
            if (leitura == null) return;

            var detalhes = $"""
                Número da Moto: #{leitura.NumeroMoto}
                Horário: {leitura.Timestamp:dd/MM/yyyy HH:mm:ss.fff}
                Tipo: {leitura.Tipo}
                Etapa: {leitura.IdEtapa}
                Volta: {leitura.Volta?.ToString() ?? "N/A"}
                
                Status: {(leitura.Sincronizado ? "✅ Sincronizado" : "⏳ Pendente")}
                {(leitura.DataSincronizacao.HasValue ? $"Sincronizado em: {leitura.DataSincronizacao:HH:mm:ss}" : "")}
                
                Device: {leitura.DeviceId}
                Hash: {leitura.Hash}
                
                Dados Brutos:
                {leitura.DadosBrutos}
                
                {(leitura.ErroSync != null ? $"Erro: {leitura.ErroSync}" : "")}
                """;

            await Shell.Current.DisplayAlert($"Leitura #{leitura.NumeroMoto}", detalhes, "Fechar");
        }

        [RelayCommand]
        private async Task ExportarLogAsync()
        {
            await ExecuteAsync(async () =>
            {
                var leituras = await _storageService.GetLeiturasRecentesAsync(10000);

                // Gerar CSV
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("NumeroMoto;Timestamp;Tipo;IdEtapa;Volta;Sincronizado;DeviceId;Hash");

                foreach (var l in leituras)
                {
                    sb.AppendLine($"{l.NumeroMoto};{l.Timestamp:yyyy-MM-dd HH:mm:ss.fff};{l.Tipo};{l.IdEtapa};{l.Volta};{l.Sincronizado};{l.DeviceId};{l.Hash}");
                }

                // Salvar arquivo
                var fileName = $"leituras_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllTextAsync(filePath, sb.ToString());

                // Compartilhar
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Exportar Log de Leituras",
                    File = new ShareFile(filePath)
                });

            }, "Exportando...");
        }

        [RelayCommand]
        private async Task LimparHistoricoAsync()
        {
            var confirmacao = await Shell.Current.DisplayAlert(
                "⚠️ Limpar Histórico",
                "Esta ação irá apagar TODAS as leituras locais.\n\nAs leituras sincronizadas já estão salvas no servidor.\n\nDeseja continuar?",
                "Sim, Limpar",
                "Cancelar");

            if (!confirmacao) return;

            // TODO: Implementar limpeza no StorageService
            // await _storageService.LimparLeiturasAsync();

            Leituras.Clear();
            TotalLeituras = 0;
            LeiturasSincronizadas = 0;
            LeiturasPendentes = 0;

            await Shell.Current.DisplayAlert("Concluído", "Histórico limpo", "OK");
        }
    }
}