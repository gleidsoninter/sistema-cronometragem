using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Usb;
using Android.OS;
using Android.Views;

namespace AppColetor
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTask,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                              ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
    public class MainActivity : MauiAppCompatActivity
    {
        // Evento para quando USB é conectado/desconectado
        public static event EventHandler<UsbDevice?>? UsbDeviceAttached;
        public static event EventHandler<UsbDevice?>? UsbDeviceDetached;

        // Referência estática para acesso global
        public static MainActivity? Instance { get; private set; }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;

            // Manter tela ligada durante uso
            Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

            // Verificar se foi iniciado por conexão USB
            ProcessarIntentUsb(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            ProcessarIntentUsb(intent);
        }

        private void ProcessarIntentUsb(Intent? intent)
        {
            if (intent?.Action == UsbManager.ActionUsbDeviceAttached)
            {
                var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;
                if (device != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[USB] Dispositivo conectado via Intent: {device.DeviceName}");
                    UsbDeviceAttached?.Invoke(this, device);
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Registrar receiver para eventos USB
            var filter = new IntentFilter();
            filter.AddAction(UsbManager.ActionUsbDeviceAttached);
            filter.AddAction(UsbManager.ActionUsbDeviceDetached);
            RegisterReceiver(UsbEventReceiver.Instance, filter);
        }

        protected override void OnPause()
        {
            base.OnPause();

            try
            {
                UnregisterReceiver(UsbEventReceiver.Instance);
            }
            catch
            {
                // Ignorar se não estava registrado
            }
        }

        // Método para invocar eventos USB de forma segura
        public static void OnUsbAttached(UsbDevice? device) => UsbDeviceAttached?.Invoke(Instance, device);
        public static void OnUsbDetached(UsbDevice? device) => UsbDeviceDetached?.Invoke(Instance, device);
    }

    /// <summary>
    /// Receiver para eventos USB (conectar/desconectar)
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached, UsbManager.ActionUsbDeviceDetached })]
    public class UsbEventReceiver : BroadcastReceiver
    {
        public static readonly UsbEventReceiver Instance = new();

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null) return;

            var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;

            switch (intent.Action)
            {
                case UsbManager.ActionUsbDeviceAttached:
                    System.Diagnostics.Debug.WriteLine($"[USB] Dispositivo conectado: {device?.DeviceName}");
                    MainActivity.OnUsbAttached(device);
                    break;

                case UsbManager.ActionUsbDeviceDetached:
                    System.Diagnostics.Debug.WriteLine($"[USB] Dispositivo desconectado: {device?.DeviceName}");
                    MainActivity.OnUsbDetached(device);
                    break;
            }
        }
    }
}