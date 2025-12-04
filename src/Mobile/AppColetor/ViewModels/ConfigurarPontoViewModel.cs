using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppColetor.Models.DTOs;
using AppColetor.Services.Implementations;
using AppColetor.Services.Interfaces;

namespace AppColetor.ViewModels
{
    public partial class ConfigurarPontoViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly DeviceIdentityService _identityService;
        private readonly IConfigService _configService;

        public ConfigurarPontoViewModel(
            IApiService apiService,
            DeviceIdentityService identityService,
            IConfigService configService)
        {
            _apiService = apiService;
            _identityService = identityService;
            _configService = configService;

            Title = "Configurar Ponto";
        }

        // ═══════════════════════════════════════════════════════════════════
        // PROPRIEDADES
        // ═══════════════════════════════════════════════════════════════════

        public ObservableCollection<EtapaInfoDto> Etapas { get; } = new();
        public ObservableCollection<EspecialInfoDto> Especiais { get; } = new();

        [ObservableProperty]
        private EtapaInfoDto? _etapaSelecionada;

        [ObservableProperty]
        private EspecialInfoDto? _especialSelecionado;

        [ObservableProperty]
        private string _tipoSelecionado = "PASSAGEM";

        [ObservableProperty]
        private string _nomeColetor = "";

        [ObservableProperty]
        private string _senhaRegistro = "";

        [ObservableProperty]
        private string _deviceIdPreview = "";

        [ObservableProperty]
        private bool _mostrarEspeciais;

        public bool PodeRegistrar =>
            EtapaSelecionada != null &&
            !string.IsNullOrEmpty(TipoSelecionado) &&
            !string.IsNullOrEmpty(NomeColetor) &&
            !string.IsNullOrEmpty(SenhaRegistro);

        // ═══════════════════════════════════════════════════════════════════
        // COMANDOS
        // ═══════════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task CarregarAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Carregar etapas da API
                var etapas = await _apiService.ListarEtapasAtivasAsync();

                Etapas.Clear();
                foreach (var etapa in etapas)
                {
                    Etapas.Add(etapa);
                }

                // Selecionar etapa salva
                var idEtapaSalva = await _configService.GetIntAsync(Constants.KEY_ID_ETAPA);
                if (idEtapaSalva > 0)
                {
                    EtapaSelecionada = Etapas.FirstOrDefault(e => e.Id == idEtapaSalva);
                }

                AtualizarDeviceIdPreview();

            }, "Carregando...");
        }

        partial void OnEtapaSelecionadaChanged(EtapaInfoDto? value)
        {
            if (value != null)
            {
                _ = CarregarEspeciaisAsync(value.Id);
            }

            AtualizarDeviceIdPreview();
            OnPropertyChanged(nameof(PodeRegistrar));
        }

        private async Task CarregarEspeciaisAsync(int idEtapa)
        {
            var especiais = await _apiService.ListarEspeciaisAsync(idEtapa);

            Especiais.Clear();
            foreach (var especial in especiais)
            {
                Especiais.Add(especial);
            }
        }

        [RelayCommand]
        private void SelecionarTipo(string tipo)
        {
            TipoSelecionado = tipo;

            // Mostrar especiais apenas para ENTRADA e SAIDA
            MostrarEspeciais = tipo == "ENTRADA" || tipo == "SAIDA";

            if (!MostrarEspeciais)
            {
                EspecialSelecionado = null;
            }

            // Sugerir nome
            NomeColetor = tipo switch
            {
                "LARGADA" => "Largada",
                "CHEGADA" => "Chegada",
                "CONCENTRACAO" => "Concentração",
                "PASSAGEM" => "Ponto de Passagem",
                "ENTRADA" when EspecialSelecionado != null => $"{EspecialSelecionado.Nome} - Entrada",
                "SAIDA" when EspecialSelecionado != null => $"{EspecialSelecionado.Nome} - Saída",
                _ => NomeColetor
            };

            AtualizarDeviceIdPreview();
            OnPropertyChanged(nameof(PodeRegistrar));
        }

        partial void OnEspecialSelecionadoChanged(EspecialInfoDto? value)
        {
            if (value != null && (TipoSelecionado == "ENTRADA" || TipoSelecionado == "SAIDA"))
            {
                NomeColetor = $"{value.Nome} - {(TipoSelecionado == "ENTRADA" ? "Entrada" : "Saída")}";
            }

            AtualizarDeviceIdPreview();
            OnPropertyChanged(nameof(PodeRegistrar));
        }

        partial void OnNomeColetorChanged(string value)
        {
            OnPropertyChanged(nameof(PodeRegistrar));
        }

        partial void OnSenhaRegistroChanged(string value)
        {
            OnPropertyChanged(nameof(PodeRegistrar));
        }

        private void AtualizarDeviceIdPreview()
        {
            DeviceIdPreview = _identityService.GerarNovoDeviceId(
                TipoSelecionado,
                EspecialSelecionado?.Numero);
        }

        [RelayCommand]
        private async Task RegistrarAsync()
        {
            if (!PodeRegistrar)
                return;

            await ExecuteAsync(async () =>
            {
                // Configurar ponto de leitura localmente
                await _identityService.ConfigurarPontoLeituraAsync(
                    TipoSelecionado,
                    EspecialSelecionado?.Id,
                    EspecialSelecionado?.Nome,
                    NomeColetor);

                // Obter dispositivo atualizado
                var dispositivo = await _identityService.ObterOuCriarIdentidadeAsync();
                dispositivo.IdEtapa = EtapaSelecionada!.Id;

                // Registrar na API
                var resultado = await _apiService.RegistrarDispositivoAsync(
                    dispositivo,
                    SenhaRegistro);

                if (resultado?.Sucesso == true)
                {
                    // Salvar configurações
                    await _configService.SetIntAsync(Constants.KEY_ID_ETAPA, EtapaSelecionada.Id);

                    // Salvar token de forma segura
                    if (!string.IsNullOrEmpty(resultado.Token))
                    {
                        await SecureStorage.SetAsync(Constants.KEY_API_TOKEN, resultado.Token);
                        _apiService.ConfigurarToken(resultado.Token);
                    }

                    await Shell.Current.DisplayAlert(
                        "Sucesso",
                        $"Coletor registrado como:\n{NomeColetor}\n\nDevice ID: {dispositivo.DeviceId}",
                        "OK");

                    await GoBackAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Erro",
                        resultado?.Mensagem ?? "Falha ao registrar dispositivo",
                        "OK");
                }

            }, "Registrando...");
        }

        [RelayCommand]
        private async Task CancelarAsync()
        {
            await GoBackAsync();
        }
    }

    // DTO auxiliar
    public class EspecialInfoDto
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Nome { get; set; } = "";
        public double DistanciaKm { get; set; }
    }
}