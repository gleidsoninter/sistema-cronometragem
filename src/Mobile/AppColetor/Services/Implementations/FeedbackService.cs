using Android.Media;
using AppColetor.Services.Interfaces;
using Plugin.Maui.Audio;

namespace AppColetor.Services.Implementations
{
    public class FeedbackService : IFeedbackService
    {
        private readonly bool _vibrarHabilitado;
        private readonly bool _somHabilitado;

        // Áudio players (inicializados sob demanda)
        private IAudioPlayer? _beepPlayer;
        private IAudioPlayer? _errorPlayer;
        private IAudioPlayer? _successPlayer;

        public FeedbackService()
        {
            _vibrarHabilitado = Preferences.Get("vibrar_ao_ler", true);
            _somHabilitado = Preferences.Get("som_ao_ler", true);
        }

        public async Task LeituraRecebidaAsync()
        {
            var tasks = new List<Task>();

            if (_vibrarHabilitado)
            {
                tasks.Add(Task.Run(() =>
                {
                    try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50)); }
                    catch { }
                }));
            }

            if (_somHabilitado)
            {
                tasks.Add(PlayBeepAsync());
            }

            await Task.WhenAll(tasks);
        }

        public async Task LeituraSincronizadaAsync()
        {
            if (_somHabilitado)
            {
                await PlaySuccessAsync();
            }
        }

        public async Task ErroAsync()
        {
            var tasks = new List<Task>();

            if (_vibrarHabilitado)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Vibração mais longa para erro
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                        Thread.Sleep(100);
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                    }
                    catch { }
                }));
            }

            if (_somHabilitado)
            {
                tasks.Add(PlayErrorAsync());
            }

            await Task.WhenAll(tasks);
        }

        public async Task ConexaoEstabelecidaAsync()
        {
            if (_vibrarHabilitado)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                }
                catch { }
            }

            await Task.CompletedTask;
        }

        public async Task ConexaoPerdidaAsync()
        {
            if (_vibrarHabilitado)
            {
                try
                {
                    // Padrão de vibração para desconexão
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
                    await Task.Delay(150);
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
                    await Task.Delay(150);
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
                }
                catch { }
            }
        }

        private async Task PlayBeepAsync()
        {
            try
            {
                if (_beepPlayer == null)
                {
                    var audioManager = AudioManager.Current;
                    _beepPlayer = audioManager.CreatePlayer(
                        await FileSystem.OpenAppPackageFileAsync("beep.mp3"));
                }

                _beepPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Feedback] Erro ao tocar beep: {ex.Message}");
            }
        }

        private async Task PlaySuccessAsync()
        {
            try
            {
                if (_successPlayer == null)
                {
                    var audioManager = AudioManager.Current;
                    _successPlayer = audioManager.CreatePlayer(
                        await FileSystem.OpenAppPackageFileAsync("success.mp3"));
                }

                _successPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Feedback] Erro ao tocar success: {ex.Message}");
            }
        }

        private async Task PlayErrorAsync()
        {
            try
            {
                if (_errorPlayer == null)
                {
                    var audioManager = AudioManager.Current;
                    _errorPlayer = audioManager.CreatePlayer(
                        await FileSystem.OpenAppPackageFileAsync("error.mp3"));
                }

                _errorPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Feedback] Erro ao tocar error: {ex.Message}");
            }
        }
    }
}