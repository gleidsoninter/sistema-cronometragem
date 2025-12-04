using AppColetor.Helpers;
using AppColetor.Models.Entities;
using AppColetor.Services.Interfaces;

namespace AppColetor.Services.Implementations
{
    public class DeviceIdentityService
    {
        private readonly IConfigService _configService;
        private DispositivoColetor? _currentDevice;

        public DispositivoColetor? CurrentDevice => _currentDevice;

        public DeviceIdentityService(IConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Gera um novo Device ID único
        /// </summary>
        public string GerarNovoDeviceId(string tipo = "PAS", int? numeroEspecial = null)
        {
            var prefixo = tipo switch
            {
                "ENTRADA" => $"E{numeroEspecial}E",
                "SAIDA" => $"E{numeroEspecial}S",
                "PASSAGEM" => "PAS",
                "LARGADA" => "LAR",
                "CHEGADA" => "CHE",
                "CONCENTRACAO" => "CON",
                _ => "GEN"
            };

            var uuid = Guid.NewGuid().ToString("N")[..8].ToUpper();
            return $"COLETOR-{prefixo}-{uuid}";
        }

        /// <summary>
        /// Obtém ou cria a identidade do dispositivo atual
        /// </summary>
        public async Task<DispositivoColetor> ObterOuCriarIdentidadeAsync()
        {
            if (_currentDevice != null)
                return _currentDevice;

            // Tentar carregar existente
            var deviceId = await _configService.GetStringAsync(Constants.KEY_DEVICE_ID);

            if (!string.IsNullOrEmpty(deviceId))
            {
                _currentDevice = await CarregarDispositivoAsync(deviceId);

                if (_currentDevice != null)
                {
                    // Atualizar informações do dispositivo
                    await AtualizarInfoDispositivoAsync(_currentDevice);
                    return _currentDevice;
                }
            }

            // Criar novo dispositivo
            _currentDevice = await CriarNovoDispositivoAsync();
            return _currentDevice;
        }

        private async Task<DispositivoColetor?> CarregarDispositivoAsync(string deviceId)
        {
            // Carregar do armazenamento local
            var nome = await _configService.GetStringAsync("device_nome");
            var tipo = await _configService.GetStringAsync("device_tipo");
            var idEspecial = await _configService.GetIntAsync("device_id_especial");
            var nomeEspecial = await _configService.GetStringAsync("device_nome_especial");
            var idEtapa = await _configService.GetIntAsync(Constants.KEY_ID_ETAPA);

            return new DispositivoColetor
            {
                DeviceId = deviceId,
                Nome = nome ?? "Coletor",
                Tipo = tipo ?? "PASSAGEM",
                IdEspecial = idEspecial > 0 ? idEspecial : null,
                NomeEspecial = nomeEspecial,
                IdEtapa = idEtapa,
                AndroidId = ObterAndroidId(),
                ModeloDispositivo = DeviceInfo.Model,
                Fabricante = DeviceInfo.Manufacturer,
                VersaoApp = AppInfo.VersionString
            };
        }

        private async Task<DispositivoColetor> CriarNovoDispositivoAsync()
        {
            var deviceId = GerarNovoDeviceId();

            var dispositivo = new DispositivoColetor
            {
                DeviceId = deviceId,
                Nome = "Novo Coletor",
                Tipo = "PASSAGEM",
                AndroidId = ObterAndroidId(),
                ModeloDispositivo = DeviceInfo.Model,
                Fabricante = DeviceInfo.Manufacturer,
                VersaoApp = AppInfo.VersionString,
                DataRegistro = DateTime.UtcNow
            };

            // Salvar localmente
            await _configService.SetStringAsync(Constants.KEY_DEVICE_ID, deviceId);

            return dispositivo;
        }

        /// <summary>
        /// Configura o dispositivo para um ponto de leitura específico
        /// </summary>
        public async Task ConfigurarPontoLeituraAsync(
            string tipo,
            int? idEspecial,
            string? nomeEspecial,
            string nomeAmigavel)
        {
            if (_currentDevice == null)
                await ObterOuCriarIdentidadeAsync();

            _currentDevice!.Tipo = tipo;
            _currentDevice.IdEspecial = idEspecial;
            _currentDevice.NomeEspecial = nomeEspecial;
            _currentDevice.Nome = nomeAmigavel;

            // Gerar novo DeviceId baseado na configuração
            var novoDeviceId = GerarNovoDeviceId(tipo, idEspecial);
            _currentDevice.DeviceId = novoDeviceId;

            // Salvar
            await _configService.SetStringAsync(Constants.KEY_DEVICE_ID, novoDeviceId);
            await _configService.SetStringAsync("device_nome", nomeAmigavel);
            await _configService.SetStringAsync("device_tipo", tipo);
            await _configService.SetIntAsync("device_id_especial", idEspecial ?? 0);
            await _configService.SetStringAsync("device_nome_especial", nomeEspecial ?? "");

            System.Diagnostics.Debug.WriteLine(
                $"[Identity] Dispositivo configurado: {novoDeviceId} - {nomeAmigavel}");
        }

        private async Task AtualizarInfoDispositivoAsync(DispositivoColetor dispositivo)
        {
            dispositivo.ModeloDispositivo = DeviceInfo.Model;
            dispositivo.Fabricante = DeviceInfo.Manufacturer;
            dispositivo.VersaoApp = AppInfo.VersionString;

            // Atualizar bateria
            try
            {
                dispositivo.NivelBateria = (int)(Battery.Default.ChargeLevel * 100);
            }
            catch { }
        }

        private string ObterAndroidId()
        {
            try
            {
#if ANDROID
                var context = Android.App.Application.Context;
                return Android.Provider.Settings.Secure.GetString(
                    context.ContentResolver,
                    Android.Provider.Settings.Secure.AndroidId) ?? "";
#else
                return Guid.NewGuid().ToString("N")[..16];
#endif
            }
            catch
            {
                return Guid.NewGuid().ToString("N")[..16];
            }
        }

        /// <summary>
        /// Obtém informações para o heartbeat
        /// </summary>
        public async Task<HeartbeatInfo> ObterInfoHeartbeatAsync(int leiturasPendentes, int totalLeituras)
        {
            if (_currentDevice == null)
                await ObterOuCriarIdentidadeAsync();

            int bateria = 0;
            try
            {
                bateria = (int)(Battery.Default.ChargeLevel * 100);
            }
            catch { }

            return new HeartbeatInfo
            {
                DeviceId = _currentDevice!.DeviceId,
                Nome = _currentDevice.Nome,
                Tipo = _currentDevice.Tipo,
                IdEspecial = _currentDevice.IdEspecial,
                IdEtapa = _currentDevice.IdEtapa,
                NivelBateria = bateria,
                LeiturasPendentes = leiturasPendentes,
                TotalLeiturasSessao = totalLeituras,
                VersaoApp = AppInfo.VersionString,
                ModeloDispositivo = DeviceInfo.Model
            };
        }
    }

    public class HeartbeatInfo
    {
        public string DeviceId { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int? IdEspecial { get; set; }
        public int IdEtapa { get; set; }
        public int NivelBateria { get; set; }
        public int LeiturasPendentes { get; set; }
        public int TotalLeiturasSessao { get; set; }
        public string VersaoApp { get; set; } = "";
        public string ModeloDispositivo { get; set; } = "";
    }
}