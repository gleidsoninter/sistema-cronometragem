using System.Text.Json;
using AppColetor.Helpers;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class StateService
    {
        private readonly IConfigService _configService;
        private readonly IStorageService _storageService;

        private const string KEY_LAST_SESSION = "last_session";
        private const string KEY_PENDING_COUNT = "pending_count";
        private const string KEY_LAST_SYNC = "last_sync";
        private const string KEY_USB_DEVICE = "last_usb_device";

        public StateService(IConfigService configService, IStorageService storageService)
        {
            _configService = configService;
            _storageService = storageService;
        }

        /// <summary>
        /// Salva estado da sessão atual
        /// </summary>
        public async Task SalvarEstadoSessaoAsync(SessionState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state);
                await _configService.SetStringAsync(KEY_LAST_SESSION, json);

                System.Diagnostics.Debug.WriteLine("[State] Estado da sessão salvo");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[State] Erro ao salvar estado: {ex.Message}");
            }
        }

        /// <summary>
        /// Restaura estado da última sessão
        /// </summary>
        public async Task<SessionState?> RestaurarEstadoSessaoAsync()
        {
            try
            {
                var json = await _configService.GetStringAsync(KEY_LAST_SESSION);

                if (string.IsNullOrEmpty(json))
                    return null;

                var state = JsonSerializer.Deserialize<SessionState>(json);

                System.Diagnostics.Debug.WriteLine("[State] Estado da sessão restaurado");

                return state;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[State] Erro ao restaurar estado: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Salva informações do último dispositivo USB usado
        /// </summary>
        public async Task SalvarUltimoDispositivoUsbAsync(UsbDeviceState device)
        {
            var json = JsonSerializer.Serialize(device);
            await _configService.SetStringAsync(KEY_USB_DEVICE, json);
        }

        /// <summary>
        /// Obtém informações do último dispositivo USB
        /// </summary>
        public async Task<UsbDeviceState?> ObterUltimoDispositivoUsbAsync()
        {
            var json = await _configService.GetStringAsync(KEY_USB_DEVICE);

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<UsbDeviceState>(json);
        }

        /// <summary>
        /// Verifica se há leituras pendentes de sessão anterior
        /// </summary>
        public async Task<int> VerificarLeiturasNaoSincronizadasAsync()
        {
            return await _storageService.ContarLeiturasNaoSincronizadasAsync();
        }

        /// <summary>
        /// Limpa estado da sessão
        /// </summary>
        public async Task LimparEstadoAsync()
        {
            await _configService.SetStringAsync(KEY_LAST_SESSION, "");
        }
    }

    public class SessionState
    {
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
        public int IdEtapa { get; set; }
        public string? DeviceId { get; set; }
        public int TotalLeituras { get; set; }
        public int LeiturasPendentes { get; set; }
        public DateTime? UltimoSync { get; set; }
        public bool UsbConectado { get; set; }
        public string? UltimoErro { get; set; }
    }

    public class UsbDeviceState
    {
        public string? DeviceId { get; set; }
        public string? Nome { get; set; }
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public int BaudRate { get; set; }
    }
}