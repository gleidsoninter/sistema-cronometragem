using System.Text;

namespace AppColetor.Helpers
{
    public static class DiagnosticoHelper
    {
        public static async Task<string> GerarRelatorioAsync()
        {
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("     RELATÓRIO DE DIAGNÓSTICO");
            sb.AppendLine($"     {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();

            // Info do dispositivo
            sb.AppendLine("📱 DISPOSITIVO");
            sb.AppendLine($"   Modelo: {DeviceInfo.Model}");
            sb.AppendLine($"   Fabricante: {DeviceInfo.Manufacturer}");
            sb.AppendLine($"   SO: {DeviceInfo.Platform} {DeviceInfo.VersionString}");
            sb.AppendLine($"   Tipo: {DeviceInfo.DeviceType}");
            sb.AppendLine();

            // Info do app
            sb.AppendLine("📦 APLICATIVO");
            sb.AppendLine($"   Versão: {AppInfo.VersionString}");
            sb.AppendLine($"   Build: {AppInfo.BuildString}");
            sb.AppendLine($"   Package: {AppInfo.PackageName}");
            sb.AppendLine();

            // Conectividade
            sb.AppendLine("🌐 CONECTIVIDADE");
            var connectivity = Connectivity.Current;
            sb.AppendLine($"   Status: {connectivity.NetworkAccess}");
            sb.AppendLine($"   Tipos: {string.Join(", ", connectivity.ConnectionProfiles)}");
            sb.AppendLine();

            // Bateria
            sb.AppendLine("🔋 BATERIA");
            try
            {
                sb.AppendLine($"   Nível: {Battery.Default.ChargeLevel * 100:F0}%");
                sb.AppendLine($"   Estado: {Battery.Default.State}");
                sb.AppendLine($"   Fonte: {Battery.Default.PowerSource}");
            }
            catch
            {
                sb.AppendLine("   Informação não disponível");
            }
            sb.AppendLine();

            // Armazenamento
            sb.AppendLine("💾 ARMAZENAMENTO");
            sb.AppendLine($"   App Data: {FileSystem.AppDataDirectory}");
            sb.AppendLine($"   Cache: {FileSystem.CacheDirectory}");
            sb.AppendLine();

            // Configurações do app
            sb.AppendLine("⚙️ CONFIGURAÇÕES");
            sb.AppendLine($"   API URL: {Preferences.Get(Constants.KEY_API_URL, "não configurada")}");
            sb.AppendLine($"   Device ID: {Preferences.Get(Constants.KEY_DEVICE_ID, "não configurado")}");
            sb.AppendLine($"   ID Etapa: {Preferences.Get(Constants.KEY_ID_ETAPA, 0)}");
            sb.AppendLine($"   Baud Rate: {Preferences.Get(Constants.KEY_BAUD_RATE, 115200)}");
            sb.AppendLine($"   Protocolo: {Preferences.Get(Constants.KEY_PROTOCOLO, "GENERICO")}");
            sb.AppendLine();

            sb.AppendLine("═══════════════════════════════════════");

            return sb.ToString();
        }

        public static async Task CompartilharRelatorioAsync()
        {
            var relatorio = await GerarRelatorioAsync();

            var fileName = $"diagnostico_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, relatorio);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartilhar Diagnóstico",
                File = new ShareFile(filePath)
            });
        }
    }
}