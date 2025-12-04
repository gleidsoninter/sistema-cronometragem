using AppColetor.Services.Implementations;
using AppColetor.Services.Interfaces;

namespace AppColetor
{
    public partial class App : Application
    {
        private readonly StateService _stateService;
        private readonly SyncService _syncService;
        private readonly IConnectivityService _connectivityService;

        public App(
            StateService stateService,
            SyncService syncService,
            IConnectivityService connectivityService)
        {
            InitializeComponent();

            _stateService = stateService;
            _syncService = syncService;
            _connectivityService = connectivityService;

            MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            System.Diagnostics.Debug.WriteLine("[App] Iniciando...");

            // Restaurar estado da sessão anterior
            var estado = await _stateService.RestaurarEstadoSessaoAsync();

            if (estado != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[App] Sessão anterior: {estado.TotalLeituras} leituras, " +
                    $"{estado.LeiturasPendentes} pendentes");

                // Se há leituras pendentes, mostrar notificação
                if (estado.LeiturasPendentes > 0)
                {
                    // Mostrar alerta se o app foi fechado com leituras pendentes
                    await Shell.Current.DisplayAlert(
                        "Leituras Pendentes",
                        $"Há {estado.LeiturasPendentes} leituras não sincronizadas da sessão anterior.",
                        "OK");
                }
            }

            // Iniciar serviços
            _connectivityService.IniciarMonitoramento();
            _syncService.Iniciar();
        }

        protected override async void OnSleep()
        {
            base.OnSleep();

            System.Diagnostics.Debug.WriteLine("[App] Entrando em segundo plano...");

            // Salvar estado atual
            // Isso seria chamado do MainViewModel com os dados atuais
            // Por simplicidade, salvamos um estado básico aqui

            // Manter serviço de sync rodando em background
        }

        protected override void OnResume()
        {
            base.OnResume();

            System.Diagnostics.Debug.WriteLine("[App] Retomando...");

            // Verificar conectividade
            _ = _connectivityService.VerificarAlcanceApiAsync();

            // Forçar sync se estiver online
            if (_connectivityService.IsOnline && !_syncService.IsSyncing)
            {
                _ = _syncService.ExecutarSyncAsync();
            }
        }
    }
}