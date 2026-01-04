using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using RadarApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.Audio;
using Timer = System.Threading.Timer;

namespace RadarApp
{
    public partial class MapPage : ContentPage
    {
        public const double AlertRadiusMeters = 200.0;
        private const int AlertIntervalSeconds = 3;

        private enum RadarFilterType
        {
            Active,
            Today,
            AllPossible
        }

        private Location? _userLocation;
        private bool _isMapLoaded = false;
        private bool _isTrackingActive = false;
        private const double MovementThreshold = 1.0;
        private const double TrackingMovementThreshold = 0.5;

        private double _currentCompassHeading = 0;
        private Timer? _locationUpdateTimer;
        private GeolocationAccuracy _currentAccuracy = GeolocationAccuracy.Medium;
        private CancellationTokenSource? _locationListeningCts;

        private List<RadarCoordinate> _currentActiveRadars = new List<RadarCoordinate>();
        private bool _isInsideZone = false;

        private IAudioPlayer? _audioPlayer;
        private Timer? _alertLoopTimer;

        private bool _isMenuOpen = false;

        // Konstruktor - inicijalizuje stranicu, učitava mapu i pokreće locationtracking
        public MapPage()
        {
            InitializeComponent();

            MapView.Navigated += OnMapNavigated;
            MapView.Navigating += OnWebViewNavigating;

            _ = PrepareAudioAsync();

            StartLocationTimer(GeolocationAccuracy.Medium);
            _ = GetLocationAndLoadMapAsync();
        }

        // Toggle funkcija za burger meni
        private async void OnBurgerMenuClicked(object sender, EventArgs e)
        {
            if (_isMenuOpen) await CloseMenu();
            else await OpenMenu();
        }

        // Handler za X dugme - zatvara meni
        private async void OnCloseMenuClicked(object sender, EventArgs e)
        {
            await CloseMenu();
        }

        // Animirano otvaranje menija sa overlay efektom
        private async Task OpenMenu()
        {
            _isMenuOpen = true;
            BurgerMenuFrame.IsVisible = true;
            Overlay.IsVisible = true;
            var menuAnimation = BurgerMenuFrame.TranslateTo(0, 0, 250, Easing.CubicOut);
            var overlayAnimation = Overlay.FadeTo(0.5, 250);
            await Task.WhenAll(menuAnimation, overlayAnimation);
            Overlay.InputTransparent = false;

            // Dodaje tap gesture na overlay da zatvori meni kad se klikne van njega
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await CloseMenu();
            Overlay.GestureRecognizers.Clear();
            Overlay.GestureRecognizers.Add(tapGesture);
        }

        // Animirano zatvaranje menija
        private async Task CloseMenu()
        {
            _isMenuOpen = false;
            Overlay.InputTransparent = true;
            var menuAnimation = BurgerMenuFrame.TranslateTo(-250, 0, 250, Easing.CubicIn);
            var overlayAnimation = Overlay.FadeTo(0, 250);
            await Task.WhenAll(menuAnimation, overlayAnimation);
            BurgerMenuFrame.IsVisible = false;
            Overlay.IsVisible = false;
            Overlay.GestureRecognizers.Clear();
        }

        // Navigacija na Lista stranicu
        private async void OnListaClicked(object sender, EventArgs e)
        {
            await CloseMenu();
            await Shell.Current.GoToAsync("//MainPage");
        }

        // Ostaje na Mapa stranici - samo zatvara meni
        private async void OnMapaClicked(object sender, EventArgs e)
        {
            await CloseMenu();
        }

        // Priprema audio player za zvučna upozorenja
        private async Task PrepareAudioAsync()
        {
            try
            {
                var audioManager = AudioManager.Current;
                if (await FileSystem.AppPackageFileExistsAsync("beep.mp3"))
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("beep.mp3");
                    _audioPlayer = audioManager.CreatePlayer(stream);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Audio error: {ex.Message}"); }
        }

        // Hvata navigacijske evente iz WebView-a za promjenu filtera i tracking moda
        private async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            e.Cancel = true;

            // Promjena radar filtera (aktivni, današnji, svi mogući)
            if (e.Url.StartsWith("updateradarfilter://"))
            {
                string filterType = e.Url.Replace("updateradarfilter://", "");
                if (filterType == "active") await UpdateRadarPinsAsync(RadarFilterType.Active);
                else if (filterType == "today") await UpdateRadarPinsAsync(RadarFilterType.Today);
                else if (filterType == "all") await UpdateRadarPinsAsync(RadarFilterType.AllPossible);
            }
            // Uključivanje/isključivanje tracking moda
            else if (e.Url.StartsWith("updatetrackingmode://"))
            {
                bool startTracking = e.Url.Contains("true");
                _isTrackingActive = startTracking;

                if (startTracking)
                {
                    StopLocationTimer();
                    await StartContinuousLocationTracking();
                    StartCompass();
                    if (_userLocation != null) await CallUpdateUserLocationJs(_userLocation, _currentCompassHeading);
                }
                else
                {
                    StopContinuousLocationTracking();
                    StartLocationTimer(GeolocationAccuracy.Medium);
                    StopCompass();
                    StopAlertLoop();
                    _isInsideZone = false;
                    if (_userLocation != null) await CallUpdateUserLocationJs(_userLocation, 0);
                }
            }
            else { e.Cancel = false; }
        }

        // Pokreće kontinuirano praćenje lokacije (za tracking mod)
        private async Task StartContinuousLocationTracking()
        {
            StopContinuousLocationTracking();
            _locationListeningCts = new CancellationTokenSource();
            try
            {
                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
                await Geolocation.Default.StartListeningForegroundAsync(request);
                Geolocation.Default.LocationChanged += OnContinuousLocationChanged;
            }
            catch { }
        }

        // Zaustavlja kontinuirano praćenje lokacije
        private void StopContinuousLocationTracking()
        {
            try
            {
                Geolocation.Default.LocationChanged -= OnContinuousLocationChanged;
                Geolocation.Default.StopListeningForeground();
                _locationListeningCts?.Cancel();
                _locationListeningCts?.Dispose();
                _locationListeningCts = null;
            }
            catch { }
        }

        // Handler za promjene lokacije u tracking modu
        private async void OnContinuousLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            if (e.Location == null || !_isMapLoaded) return;
            var location = e.Location;

            if (_isTrackingActive)
            {
                _userLocation = location;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await CallUpdateUserLocationJs(location, _currentCompassHeading);
                    CheckProximityToRadars(location);
                });
            }
        }

        // Pokreće timer za periodično ažuriranje lokacije (normalni mod)
        private void StartLocationTimer(GeolocationAccuracy accuracy)
        {
            _currentAccuracy = accuracy;
            TimeSpan interval = accuracy == GeolocationAccuracy.High ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(2);
            _locationUpdateTimer?.Dispose();
            _locationUpdateTimer = new Timer(async (state) =>
            {
                if (_isMapLoaded && !_isTrackingActive)
                {
                    var location = await GetCurrentLocationAsync(_currentAccuracy);
                    if (location != null) MainThread.BeginInvokeOnMainThread(() => OnLocationUpdated(location));
                }
            }, null, TimeSpan.Zero, interval);
        }

        // Zaustavlja location timer
        private void StopLocationTimer() => _locationUpdateTimer?.Dispose();

        // Dobija trenutnu lokaciju korisnika
        private async Task<Location?> GetCurrentLocationAsync(GeolocationAccuracy accuracy)
        {
            try
            {
                var request = new GeolocationRequest(accuracy, TimeSpan.FromSeconds(10));
                return await Geolocation.Default.GetLocationAsync(request);
            }
            catch { return null; }
        }

        // Ažurira lokaciju ako se korisnik dovoljno pomjerio
        private async void OnLocationUpdated(Location? location)
        {
            if (location == null) return;

            if (_userLocation == null || location.CalculateDistance(_userLocation, DistanceUnits.Kilometers) * 1000 > MovementThreshold)
            {
                _userLocation = location;
                double headingToReport = _isTrackingActive ? _currentCompassHeading : 0;
                await CallUpdateUserLocationJs(location, headingToReport);
            }
        }

        // Provjerava da li je korisnik unutar alert zone nekog radara
        private void CheckProximityToRadars(Location userLocation)
        {
            bool currentlyInside = false;

            foreach (var radar in _currentActiveRadars)
            {
                double distanceKm = Location.CalculateDistance(userLocation.Latitude, userLocation.Longitude, radar.Latitude, radar.Longitude, DistanceUnits.Kilometers);
                if ((distanceKm * 1000.0) <= AlertRadiusMeters) { currentlyInside = true; break; }
            }

            if (currentlyInside && !_isInsideZone) { _isInsideZone = true; StartAlertLoop(); }
            else if (!currentlyInside && _isInsideZone) { _isInsideZone = false; StopAlertLoop(); }
        }

        // Pokreće petlju za ponavljanje alertova
        private void StartAlertLoop()
        {
            PlayBeep();
            _alertLoopTimer?.Dispose();
            _alertLoopTimer = new Timer((state) => { MainThread.BeginInvokeOnMainThread(() => PlayBeep()); }, null, TimeSpan.FromSeconds(AlertIntervalSeconds), TimeSpan.FromSeconds(AlertIntervalSeconds));
        }

        // Zaustavlja petlju alertova
        private void StopAlertLoop() { _alertLoopTimer?.Dispose(); _alertLoopTimer = null; }

        // Pusti zvučni signal i vibraciju za upozorenje
        private async void PlayBeep()
        {
            try
            {
                if (Vibration.Default.IsSupported) Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                if (_audioPlayer != null) _audioPlayer.Play();
                else await TextToSpeech.Default.SpeakAsync("Radar zona");
            }
            catch { }
        }

        // Pokreće compass za praćenje smjera
        private void StartCompass()
        {
            if (Compass.Default.IsSupported && !Compass.Default.IsMonitoring)
            {
                try { Compass.Default.ReadingChanged += OnCompassReadingChanged; Compass.Default.Start(SensorSpeed.Fastest); } catch { }
            }
        }

        // Zaustavlja compass
        private void StopCompass()
        {
            if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
            {
                try { Compass.Default.Stop(); Compass.Default.ReadingChanged -= OnCompassReadingChanged; } catch { }
            }
        }

        // Handler za promjene compass smjera
        private async void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            _currentCompassHeading = e.Reading.HeadingMagneticNorth;
            if (_isTrackingActive && _userLocation != null) await CallUpdateUserLocationJs(_userLocation, _currentCompassHeading);
        }

        // Poziva JavaScript funkciju u WebView-u da ažurira poziciju korisnika na mapi
        private async Task CallUpdateUserLocationJs(Location location, double heading)
        {
            if (!_isMapLoaded) return;

            string lngStr = location.Longitude.ToString(CultureInfo.InvariantCulture);
            string latStr = location.Latitude.ToString(CultureInfo.InvariantCulture);
            string headingStr = heading.ToString(CultureInfo.InvariantCulture);

            double speedKmh = (location.Speed ?? 0) * 3.6;
            if (speedKmh < 0) speedKmh = 0;
            string speedStr = speedKmh.ToString("F1", CultureInfo.InvariantCulture);

            var js = $"window.updateUserLocation({lngStr}, {latStr}, {headingStr}, {speedStr});";
            try { await MapView.EvaluateJavaScriptAsync(js); } catch { }
        }

        // Event kad se mapa uspješno učita
        private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Success) _isMapLoaded = true;
        }

        // Dobija dozvolu za lokaciju i učitava mapu sa inicijalnim podacima
        private async Task GetLocationAndLoadMapAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return;

                var initialLocation = await GetCurrentLocationAsync(GeolocationAccuracy.Medium);
                var htmlContent = await LoadHtmlAsync();
                _userLocation = initialLocation;

                var activeCoords = await LoadActiveRadarsAsync();
                _currentActiveRadars = activeCoords;

                if (activeCoords.Any()) htmlContent = InjectRadarPins(htmlContent, activeCoords);
                if (_userLocation != null) htmlContent = InjectInitialLocationIntoHtml(htmlContent, _userLocation);

                MapView.Source = new HtmlWebViewSource { Html = htmlContent };
            }
            catch (Exception ex) { await DisplayAlert("Greška", ex.Message, "OK"); }
        }

        // Učitava samo aktivne radare za trenutno vrijeme
        private async Task<List<RadarCoordinate>> LoadActiveRadarsAsync()
        {
            var parser = new RadarParser();
            string fileContent = await parser.ReadFromFileAsync();
            if (string.IsNullOrWhiteSpace(fileContent)) return new List<RadarCoordinate>();

            var currentTime = DateTime.Now.TimeOfDay;
            var activeCoords = new List<RadarCoordinate>();
            var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("===")) continue;
                var parts = trimmed.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length != 2) continue;

                var timePart = parts[0].Trim();
                var locationName = parts[1].Trim();

                if (IsActiveAtTime(timePart, currentTime))
                {
                    // Provjerava da li se locationName nalazi u listi PossibleNames
                    var staticMatches = RadarConfig.Coordinates
                        .Where(c => c.PossibleNames.Any(n => n.Equals(locationName, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    foreach (var sm in staticMatches)
                    {
                        activeCoords.Add(new RadarCoordinate
                        {
                            PossibleNames = sm.PossibleNames,
                            Latitude = sm.Latitude,
                            Longitude = sm.Longitude,
                            SpeedLimit = sm.SpeedLimit,
                            StartTime = timePart
                        });
                    }
                }
            }
            return activeCoords;
        }

        // Učitava sve radare koji su bili danas u listi (bez duplikata)
        private async Task<List<RadarCoordinate>> LoadAllRadarsFromListAsync()
        {
            var parser = new RadarParser();
            string fileContent = await parser.ReadFromFileAsync();
            if (string.IsNullOrWhiteSpace(fileContent)) return new List<RadarCoordinate>();

            var allCoords = new List<RadarCoordinate>();
            var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var seenLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("===")) continue;
                var parts = trimmed.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length != 2) continue;

                var timePart = parts[0].Trim();
                var locationName = parts[1].Trim();

                if (seenLocations.Add(locationName))
                {
                    // Provjerava unutar PossibleNames
                    var staticMatches = RadarConfig.Coordinates
                        .Where(c => c.PossibleNames.Any(n => n.Equals(locationName, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    foreach (var sm in staticMatches)
                    {
                        allCoords.Add(new RadarCoordinate
                        {
                            PossibleNames = sm.PossibleNames,
                            Latitude = sm.Latitude,
                            Longitude = sm.Longitude,
                            SpeedLimit = sm.SpeedLimit,
                            StartTime = timePart
                        });
                    }
                }
            }
            return allCoords;
        }

        // Učitava sve moguće radar lokacije iz config-a
        private async Task<List<RadarCoordinate>> LoadAllPossibleRadarsAsync()
        {
            return await Task.FromResult(RadarConfig.Coordinates.Select(sc => new RadarCoordinate
            {
                PossibleNames = sc.PossibleNames,
                Latitude = sc.Latitude,
                Longitude = sc.Longitude,
                SpeedLimit = sc.SpeedLimit,
                StartTime = ""
            }).ToList());
        }

        // Provjerava da li je radar aktivan za dato vrijeme
        private bool IsActiveAtTime(string timeRange, TimeSpan currentTime)
        {
            try
            {
                var parts = timeRange.Split(new[] { " do " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) return false;
                if (!TimeSpan.TryParse(parts[0].Trim(), out TimeSpan startTime)) return false;
                if (!TimeSpan.TryParse(parts[1].Trim(), out TimeSpan endTime)) return false;
                return currentTime >= startTime && currentTime <= endTime;
            }
            catch { return false; }
        }

        // Ubacuje radar pinove u HTML kao JavaScript varijablu
        private string InjectRadarPins(string html, List<RadarCoordinate> coords)
        {
            var dto = coords.Select(c => new
            {
                longitude = c.Longitude,
                latitude = c.Latitude,
                name = c.MainName,
                description = c.StartTime,
                speedLimit = c.SpeedLimit
            }).ToList();

            var json = JsonSerializer.Serialize(dto);
            var sb = $"window._initialRadars = {json};";
            var target = "map.on('load', function () {";
            int idx = html.IndexOf(target);
            return idx != -1 ? html.Insert(idx, $"\n{sb}\n") : html;
        }

        // Ubacuje inicijalnu lokaciju korisnika u HTML
        private string InjectInitialLocationIntoHtml(string html, Location location)
        {
            string lngStr = location.Longitude.ToString(CultureInfo.InvariantCulture);
            string latStr = location.Latitude.ToString(CultureInfo.InvariantCulture);
            string inner = $@"window._userMarker = new mapboxgl.Marker({{ color: '#0000FF' }}).setLngLat([{lngStr}, {latStr}]).addTo(map); map.setCenter([{lngStr}, {latStr}]); map.setZoom(14);";
            var loadStart = "map.on('load', function () {";
            return html.Replace(loadStart, $"{loadStart}\n{inner}");
        }

        // Ažurira radar pinove na mapi prema odabranom filteru
        private async Task UpdateRadarPinsAsync(RadarFilterType filterType)
        {
            if (!_isMapLoaded) return;

            List<RadarCoordinate> coords;
            switch (filterType)
            {
                case RadarFilterType.Active: coords = await LoadActiveRadarsAsync(); _currentActiveRadars = coords; break;
                case RadarFilterType.Today: coords = await LoadAllRadarsFromListAsync(); break;
                case RadarFilterType.AllPossible: coords = await LoadAllPossibleRadarsAsync(); break;
                default: coords = new List<RadarCoordinate>(); break;
            }

            var dto = coords.Select(c => new
            {
                longitude = c.Longitude,
                latitude = c.Latitude,
                name = c.MainName,
                description = c.StartTime,
                speedLimit = c.SpeedLimit
            }).ToList();

            var json = JsonSerializer.Serialize(dto);
            var js = $"window._initialRadars = {json}; try {{ renderRadars(window._initialRadars); }} catch(e) {{}}";
            try { await MapView.EvaluateJavaScriptAsync(js); } catch { }
        }

        // Čisti resurse kad korisnik napusti stranicu
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MapView.Navigated -= OnMapNavigated;
            MapView.Navigating -= OnWebViewNavigating;
            StopLocationTimer();
            StopContinuousLocationTracking();
            StopCompass();
            StopAlertLoop();
        }

        // Učitava HTML fajl iz app package-a
        private async Task<string> LoadHtmlAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}