using Microsoft.Maui.Devices.Sensors;
using Plugin.Maui.Audio;
using RadarApp.Models;
using Timer = System.Threading.Timer;

namespace RadarApp.Services
{
    public class RadarAlertService : IDisposable
    {
        public const double AlertRadiusMeters = 200.0;
        private const int AlertIntervalSeconds = 3;

        private List<RadarCoordinate> _activeRadars = new List<RadarCoordinate>();
        private bool _isInsideZone = false;
        private IAudioPlayer? _audioPlayer;
        private Timer? _alertLoopTimer;
        public event EventHandler<int>? SpeedLimitChanged;

        // Eventi
        public event EventHandler<bool>? InsideZoneChanged;

        public bool IsInsideZone => _isInsideZone;

        public RadarAlertService()
        {
            _ = PrepareAudioAsync();
        }

        public void SetActiveRadars(List<RadarCoordinate> radars)
        {
            _activeRadars = radars ?? new List<RadarCoordinate>();
        }
        public void CheckProximity(Location userLocation)
        {
            if (userLocation == null || _activeRadars == null || !_activeRadars.Any())
            {
                if (_isInsideZone) ExitZone();
                return;
            }

            var nearestRadar = _activeRadars
                .Select(radar => new {
                    radar,
                    distance = Location.CalculateDistance(
                        userLocation.Latitude, userLocation.Longitude,
                        radar.Latitude, radar.Longitude,
                        DistanceUnits.Kilometers) * 1000.0
                })
                .Where(x => x.distance <= AlertRadiusMeters)
                .OrderBy(x => x.distance)
                .FirstOrDefault();

            if (nearestRadar != null && !_isInsideZone)
            {
                EnterZone(nearestRadar.radar.SpeedLimit);
            }
            else if (nearestRadar == null && _isInsideZone)
            {
                ExitZone();
            }
        }
        public void StopAlerts()
        {
            if (_isInsideZone)
            {
                ExitZone();
            }
        }

      private void EnterZone(int speedLimit = 0)
    {
        _isInsideZone = true;
        InsideZoneChanged?.Invoke(this, true);
        SpeedLimitChanged?.Invoke(this, speedLimit);
        StartAlertLoop();
    }

    private void ExitZone()
    {
        _isInsideZone = false;
        InsideZoneChanged?.Invoke(this, false);
        SpeedLimitChanged?.Invoke(this, 0);
        StopAlertLoop();
    }
        private void StartAlertLoop()
        {
            PlayBeep();
            _alertLoopTimer?.Dispose();
            _alertLoopTimer = new Timer(
                (state) => MainThread.BeginInvokeOnMainThread(PlayBeep),
                null,
                TimeSpan.FromSeconds(AlertIntervalSeconds),
                TimeSpan.FromSeconds(AlertIntervalSeconds));
        }

        private void StopAlertLoop()
        {
            _alertLoopTimer?.Dispose();
            _alertLoopTimer = null;
        }

        private async void PlayBeep()
        {
            try
            {

                if (Vibration.Default.IsSupported)
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                }

                if (_audioPlayer != null)
                {
                    _audioPlayer.Play();
                }
                else
                {
                    await TextToSpeech.Default.SpeakAsync("Radar zona");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri reprodukciji audio alerta: {ex.Message}");
            }
        }

        private async Task PrepareAudioAsync()
        {
            try
            {
                if (await FileSystem.AppPackageFileExistsAsync("beep.mp3"))
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("beep.mp3");
                    _audioPlayer = AudioManager.Current.CreatePlayer(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri pripremi audio fajla: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopAlertLoop();
            _audioPlayer?.Dispose();
        }
    }
}