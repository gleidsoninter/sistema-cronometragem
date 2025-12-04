using AppColetor.Services.Interfaces;

namespace AppColetor.Views.Controls
{
    public partial class DispositivoSelectorPopup : ContentView
    {
        private readonly ISerialService _serialService;
        private TaskCompletionSource<SerialDeviceInfo?>? _tcs;

        public DispositivoSelectorPopup(ISerialService serialService)
        {
            InitializeComponent();
            _serialService = serialService;

            ListaDispositivos.SelectionChanged += OnSelectionChanged;
        }

        public async Task<SerialDeviceInfo?> ShowAsync()
        {
            _tcs = new TaskCompletionSource<SerialDeviceInfo?>();

            IsVisible = true;
            await CarregarDispositivosAsync();

            return await _tcs.Task;
        }

        private async Task CarregarDispositivosAsync()
        {
            try
            {
                BtnAtualizar.IsEnabled = false;
                var dispositivos = await _serialService.ListarDispositivosAsync();
                ListaDispositivos.ItemsSource = dispositivos;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao listar dispositivos: {ex.Message}", "OK");
            }
            finally
            {
                BtnAtualizar.IsEnabled = true;
            }
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            BtnConectar.IsEnabled = e.CurrentSelection.Count > 0;
        }

        private async void OnAtualizarClicked(object? sender, EventArgs e)
        {
            await CarregarDispositivosAsync();
        }

        private void OnCancelarClicked(object? sender, EventArgs e)
        {
            IsVisible = false;
            _tcs?.TrySetResult(null);
        }

        private void OnConectarClicked(object? sender, EventArgs e)
        {
            var dispositivo = ListaDispositivos.SelectedItem as SerialDeviceInfo;
            IsVisible = false;
            _tcs?.TrySetResult(dispositivo);
        }
    }
}