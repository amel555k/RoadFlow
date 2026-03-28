using Microsoft.Maui.Devices.Sensors;
using System.Globalization;

namespace RoadFlow.Services
{
    public class LocationTrackingService : IDisposable
    {
        private const double MovementThreshold = 1.0;
        
        private Location? _currentLocation;
        private double _currentCompassHeading = 0;
        private bool _isTrackingActive = false;
        private Timer? _locationUpdateTimer;
        private CancellationTokenSource? _locationListeningCts;
        private GeolocationAccuracy _currentAccuracy = GeolocationAccuracy.Medium;

        // Eventi
        public event EventHandler<Location>? LocationChanged;
        public event EventHandler<double>? CompassChanged;

        public Location? CurrentLocation => _currentLocation;
        public void SetInitialLocation(Location location)
        {
            _currentLocation = location;
        }
        public double CurrentHeading => _currentCompassHeading;
        public bool IsTrackingActive => _isTrackingActive;
        public async Task StartContinuousTrackingAsync()
        {
            try
            {
                _isTrackingActive = true;
                _locationListeningCts = new CancellationTokenSource();
                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
                await Geolocation.Default.StartListeningForegroundAsync(request);

                Geolocation.Default.LocationChanged += OnLocationChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri pokretanju tracking-a: {ex.Message}");
                throw;
            }
        }
        public void StopContinuousTracking()
        {
            try
            {
                _isTrackingActive = false;
                _locationListeningCts?.Cancel();
                Geolocation.Default.LocationChanged -= OnLocationChanged;
                Geolocation.Default.StopListeningForeground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri zaustavljanju tracking-a: {ex.Message}");
            }
        }
        public void StartPeriodicLocationUpdates(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium, int intervalSeconds = 5)
        {
            StopPeriodicLocationUpdates();
            _currentAccuracy = accuracy;
            
            _locationUpdateTimer = new Timer(async _ =>
            {
                try
                {
                    var location = await GetCurrentLocationAsync(_currentAccuracy);
                    if (location != null)
                    {
                        _currentLocation = location;
                        LocationChanged?.Invoke(this, location);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Greška pri periodic update-u: {ex.Message}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
        }

        public void StopPeriodicLocationUpdates()
        {
            _locationUpdateTimer?.Dispose();
            _locationUpdateTimer = null;
        }

        public async Task<Location?> GetCurrentLocationAsync(GeolocationAccuracy accuracy = GeolocationAccuracy.Medium)
        {
            try
            {
                var request = new GeolocationRequest(accuracy, TimeSpan.FromSeconds(10));
                return await Geolocation.Default.GetLocationAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri dobijanju lokacije: {ex.Message}");
                return null;
            }
        }
        public void StartCompass()
        {
            if (Compass.Default.IsSupported && !Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged += OnCompassReadingChanged;
                Compass.Default.Start(SensorSpeed.UI);
            }
        }
        public void StopCompass()
        {
            if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
            {
                Compass.Default.Stop();
                Compass.Default.ReadingChanged -= OnCompassReadingChanged;
            }
        }

        public async Task<bool> RequestLocationPermissionAsync()
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
        }

        private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            if (e.Location == null) return;

            var newLocation = e.Location;
            if (_currentLocation == null || 
                Location.CalculateDistance(_currentLocation, newLocation, DistanceUnits.Kilometers) * 1000 > MovementThreshold)
            {
                _currentLocation = newLocation;
                LocationChanged?.Invoke(this, newLocation);
            }
        }

        private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            _currentCompassHeading = e.Reading.HeadingMagneticNorth;
            CompassChanged?.Invoke(this, _currentCompassHeading);
        }

        public void Dispose()
        {
            StopContinuousTracking();
            StopPeriodicLocationUpdates();
            StopCompass();
            _locationListeningCts?.Dispose();
        }
    }
}