using AppColetor.Data;
using AppColetor.Services.Implementations;
using AppColetor.Services.Interfaces;
using AppColetor.Services.Testing;
using AppColetor.ViewModels;
using AppColetor.Views.Pages;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace AppColetor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // ═══════════════════════════════════════════════════════════════
            // LOGGING
            // ═══════════════════════════════════════════════════════════════

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ═══════════════════════════════════════════════════════════════
            // DATABASE
            // ═══════════════════════════════════════════════════════════════

            builder.Services.AddSingleton<AppDatabase>();

            // ═══════════════════════════════════════════════════════════════
            // SERVICES
            // ═══════════════════════════════════════════════════════════════
            // Services de conectividade e sync
            builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
            builder.Services.AddSingleton<IQueueService, QueueService>();
            builder.Services.AddSingleton<SyncService>();
            builder.Services.AddSingleton<StateService>();
            builder.Services.AddSingleton<ConflictResolver>();

            builder.Services.AddSingleton<IConfigService, ConfigService>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<IFeedbackService, FeedbackService>();
            builder.Services.AddSingleton<IParserService, ParserService>();
            builder.Services.AddSingleton<IApiService, ApiService>();
            builder.Services.AddSingleton<SyncBackgroundService>();

            builder.Services.AddSingleton<StressTestService>();
            builder.Services.AddSingleton<ReconnectionTestService>();
            builder.Services.AddSingleton<OfflineSyncTestService>();

            builder.Services.AddSingleton<BatteryOptimizationService>();
            builder.Services.AddSingleton<DataOptimizationService>();
            builder.Services.AddSingleton<LoggingService>();

            // Serial Service - implementação Android
#if ANDROID
#if DEBUG
            // Em debug, permitir escolher entre mock e real
            var useMock = Preferences.Get("use_mock_serial", false);
            if (useMock)
            {
                builder.Services.AddSingleton<ISerialService, MockSerialService>();
            }
            else
            {
                builder.Services.AddSingleton<ISerialService, Platforms.Android.Services.AndroidSerialService>();
            }
#else
    // Em release, sempre usar real
    builder.Services.AddSingleton<ISerialService, Platforms.Android.Services.AndroidSerialService>();
#endif
#endif

            // ═══════════════════════════════════════════════════════════════
            // VIEWMODELS
            // ═══════════════════════════════════════════════════════════════

            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<ConfiguracaoViewModel>();
            builder.Services.AddTransient<HistoricoViewModel>();
            builder.Services.AddTransient<LoginViewModel>();

            // ═══════════════════════════════════════════════════════════════
            // PAGES
            // ═══════════════════════════════════════════════════════════════

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ConfiguracaoPage>();
            builder.Services.AddTransient<HistoricoPage>();
            builder.Services.AddTransient<LoginPage>();

            return builder.Build();
        }
    }

    // Mock para compilar em outras plataformas
#if !ANDROID
    public class MockSerialService : ISerialService
    {
        public event EventHandler<SerialDataEventArgs>? DataReceived;
        public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event EventHandler<SerialErrorEventArgs>? ErrorOccurred;
        
        public bool IsConnected => false;
        public string? DeviceName => null;
        
        public Task<List<SerialDeviceInfo>> ListarDispositivosAsync() => Task.FromResult(new List<SerialDeviceInfo>());
        public Task<bool> SolicitarPermissaoAsync(SerialDeviceInfo device) => Task.FromResult(false);
        public Task<bool> ConectarAsync(SerialDeviceInfo device, SerialConfig config) => Task.FromResult(false);
        public Task DesconectarAsync() => Task.CompletedTask;
        public Task EnviarAsync(string dados) => Task.CompletedTask;
        public Task EnviarAsync(byte[] dados) => Task.CompletedTask;
    }
#endif
}