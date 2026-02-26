using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using RadarApp.Services;
using RadarApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadarApp
{
    public partial class MainPage : ContentPage
    {

        private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("updateradarfilter://"))
            {
                e.Cancel = true;

                string filterType = e.Url.Replace("updateradarfilter://", "");

                var filter = filterType switch
                {
                    "active" => MapDataService.RadarFilterType.Active,
                    "today" => MapDataService.RadarFilterType.Today,
                    _ => MapDataService.RadarFilterType.Active
                };

                await UpdateRadarPinsAsync(filter);
            }
            else if (e.Url.StartsWith("updatetrackingmode://"))
            {
                e.Cancel = true;

                bool startTracking = e.Url.ToLower().Contains("true");
                await HandleTrackingModeToggle(startTracking);
            }
        }

     private async Task HandleTrackingModeToggle(bool startTracking)
{
    _isTrackingActive = startTracking;

    if (startTracking)
    {
        _locationService.StopPeriodicLocationUpdates();

    
        _= _locationService.StartContinuousTrackingAsync();

        var location = _locationService.CurrentLocation;

        if (location == null)
        {
            location = await Geolocation.GetLastKnownLocationAsync();

            if (location == null)
            {
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(1));
                    location = await Geolocation.GetLocationAsync(request);
                }
                catch (Exception ex) 
                {
                    System.Diagnostics.Debug.WriteLine($"Greška pri traženju prve lokacije: {ex.Message}");
                }
            }
        }

        if (location != null)
        {
            double initialHeading = _locationService.CurrentHeading;
            await UpdateUserLocationOnMap(location, initialHeading);
        }
    }
    else
    {
        _locationService.StopContinuousTracking();
        _locationService.StartPeriodicLocationUpdates();
        _alertService.StopAlerts();
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await MapView.EvaluateJavaScriptAsync("window.showSpeedLimit(0);");
        });

        if (_locationService.CurrentLocation != null)
            await UpdateUserLocationOnMap(_locationService.CurrentLocation, 0);
    }
}
        private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Success)
            {
                _isMapLoaded = true;
            }
        }


        private async Task InitializeMapAsync()
        {
            try
            {
                var hasPermission = await _locationService.RequestLocationPermissionAsync();

                if (!hasPermission)
                {
                    await DisplayAlert(
                        "Dozvola",
                        "Potrebna je dozvola za pristup lokaciji.",
                        "OK");
                    return;
                }
                _locationService.StartCompass();


               var currentLocation = await _locationService.GetCurrentLocationAsync(GeolocationAccuracy.Medium);

                if (currentLocation != null)
                {
                    _locationService.SetInitialLocation(currentLocation);
                }

                var htmlContent = await _mapDataService.LoadHtmlAsync();

                var activeRadars = await _mapDataService.LoadActiveRadarsAsync();

                _alertService.SetActiveRadars(activeRadars);

                if (activeRadars.Any())
                {
                    htmlContent = _mapDataService
                        .InjectRadarPins(htmlContent, activeRadars);
                }

                if (currentLocation != null)
                {
                    htmlContent = _mapDataService
                        .InjectInitialLocation(htmlContent, currentLocation);
                }

                MapView.Source = new HtmlWebViewSource
                {
                    Html = htmlContent
                };
                _alertService.SpeedLimitChanged += async (s, limit) =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MapView.EvaluateJavaScriptAsync($"window.showSpeedLimit({limit});");
                    });
                };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async Task UpdateRadarPinsAsync(
            MapDataService.RadarFilterType filterType)
        {
            var coords = await _mapDataService
                .LoadRadarsByFilterAsync(filterType);

            if (filterType == MapDataService.RadarFilterType.Active)
            {
                _alertService.SetActiveRadars(coords);
            }

            string js = _mapDataService
                .GenerateRadarRenderScript(coords);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await MapView.EvaluateJavaScriptAsync(js);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Greška pri ažuriranju radar pinova: {ex.Message}");
                }
            });
        }

        private async Task UpdateUserLocationOnMap(
            Location location,
            double heading)
        {
            if (!_isMapLoaded)
                return;

            string js = _mapDataService
                .GenerateUpdateLocationScript(location, heading);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await MapView.EvaluateJavaScriptAsync(js);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Greška pri ažuriranju lokacije: {ex.Message}");
                }
            });
        }

        private async Task OnLocationUpdated(Location location)
        {
            await UpdateUserLocationOnMap(
                location,
                _isTrackingActive
                    ? _locationService.CurrentHeading
                    : 0);

            if (_isTrackingActive)
            {
                _alertService.CheckProximity(location);
            }
        }
    }
}
