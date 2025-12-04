using AppColetor.ViewModels;

namespace AppColetor.Views.Pages
{
    public partial class ConfiguracaoPage : ContentPage
    {
        public ConfiguracaoPage(ConfiguracaoViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is ConfiguracaoViewModel vm)
            {
                await vm.CarregarCommand.ExecuteAsync(null);
            }
        }
    }
}