using CommunityToolkit.Mvvm.ComponentModel;

namespace AppColetor.ViewModels
{
    /// <summary>
    /// ViewModel base com funcionalidades comuns
    /// </summary>
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = "";

        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Executa uma ação com tratamento de busy e erros
        /// </summary>
        protected async Task ExecuteAsync(Func<Task> action, string? busyMessage = null)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = "";

                if (!string.IsNullOrEmpty(busyMessage))
                    StatusMessage = busyMessage;

                await action();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ERRO] {ex}");

                await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Exibe toast/snackbar (requer CommunityToolkit.Maui)
        /// </summary>
        protected async Task ShowToastAsync(string message)
        {
            // Usando DisplayAlert como fallback simples
            // Em produção, usar CommunityToolkit.Maui Toast
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("", message, "OK");
            });
        }

        /// <summary>
        /// Navega para uma página
        /// </summary>
        protected async Task NavigateToAsync(string route, Dictionary<string, object>? parameters = null)
        {
            if (parameters != null)
                await Shell.Current.GoToAsync(route, parameters);
            else
                await Shell.Current.GoToAsync(route);
        }

        /// <summary>
        /// Volta para página anterior
        /// </summary>
        protected async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}