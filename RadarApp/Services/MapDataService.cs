using Microsoft.Maui.Devices.Sensors;
using RadarApp.Models;
using System.Globalization;
using System.Text.Json;

namespace RadarApp.Services
{
    public class MapDataService
    {
        public enum RadarFilterType
        {
            Active,
            Today,
            AllPossible
        }

        private readonly RadarParser _parser;

        public MapDataService()
        {
            _parser = new RadarParser();
        }

        public async Task<List<RadarCoordinate>> LoadActiveRadarsAsync()
        {
            string content = await _parser.ReadFromFileAsync();
            var currentTime = DateTime.Now.TimeOfDay;
            var list = new List<RadarCoordinate>();

            if (string.IsNullOrEmpty(content))
                return list;

            foreach (var line in content.Split('\n').Where(l => !l.StartsWith("==") && l.Contains("-")))
            {
                var parts = line.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2 && IsActiveAtTime(parts[0], currentTime))
                {
                    var coords = RadarConfig.FindCoordinatesByName(parts[1].Trim());
                    foreach (var coord in coords)
                    {
                        coord.StartTime = parts[0].Trim();
                        list.Add(coord);
                    }
                }
            }

            return list;
        }

        public async Task<List<RadarCoordinate>> LoadAllRadarsFromListAsync()
        {
            string content = await _parser.ReadFromFileAsync();
            var list = new List<RadarCoordinate>();
            var seen = new HashSet<string>();

            if (string.IsNullOrEmpty(content))
                return list;

            foreach (var line in content.Split('\n').Where(l => !l.StartsWith("==") && l.Contains("-")))
            {
                var parts = line.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2 && seen.Add(parts[1].Trim()))
                {
                    var coords = RadarConfig.FindCoordinatesByName(parts[1].Trim());
                    foreach (var coord in coords)
                    {
                        coord.StartTime = parts[0].Trim();
                        list.Add(coord);
                    }
                }
            }

            return list;
        }
    
        public async Task<List<RadarCoordinate>> LoadRadarsByFilterAsync(RadarFilterType filterType)
        {
            return await LoadRadarsByFilterAsync(filterType, showStacionarni: true);
        }

        public async Task<List<RadarCoordinate>> LoadRadarsByFilterAsync(
            RadarFilterType filterType,
            bool showStacionarni)
        {
            var timedRadars = filterType switch
            {
                RadarFilterType.Active => await LoadActiveRadarsAsync(),
                RadarFilterType.Today => await LoadAllRadarsFromListAsync(),
                _ => new List<RadarCoordinate>()
            };

            var list = timedRadars
                .Where(c => !c.Stacionaran)
                .ToList();

            if (showStacionarni)
                AddStacionarniRadars(list);

            return list;
        }

        private void AddStacionarniRadars(List<RadarCoordinate> list)
        {
            foreach (var coord in RadarConfig.Coordinates.Where(c => c.Stacionaran))
            {
                coord.StartTime = "00:00 do 24:00";
                list.Add(coord);
            }
        }

        public string GenerateRadarRenderScript(List<RadarCoordinate> radars)
        {
            var dto = radars.Select(c => new
            {
                longitude = c.Longitude,
                latitude = c.Latitude,
                name = c.MainName,
                description = c.StartTime,
                speedLimit = c.SpeedLimit,
                stacionaran = c.Stacionaran
            });

            string json = JsonSerializer.Serialize(dto);
            return $"window._initialRadars = {json}; if(typeof renderRadars === 'function') {{ renderRadars(window._initialRadars); }}";
        }
        public string InjectRadarPins(string html, List<RadarCoordinate> coords)
        {
            var json = JsonSerializer.Serialize(coords.Select(c => new
            {
                longitude = c.Longitude,
                latitude = c.Latitude,
                name = c.MainName,
                description = c.StartTime,
                speedLimit = c.SpeedLimit,
                stacionaran = c.Stacionaran
            }));
            
            return html.Insert(html.IndexOf("map.on('load'"), $"window._initialRadars = {json};\n");
        }

        public string InjectInitialLocation(string html, Location location)
        {
           string js = $@"
                    var userEl = document.createElement('div');
                    userEl.className = 'user-marker';
                    window._userMarker = new mapboxgl.Marker({{ element: userEl, rotationAlignment: 'viewport' }})
                        .setLngLat([{location.Longitude.ToString(CultureInfo.InvariantCulture)}, {location.Latitude.ToString(CultureInfo.InvariantCulture)}])
                        .addTo(map); 
                    map.setCenter([{location.Longitude.ToString(CultureInfo.InvariantCulture)}, {location.Latitude.ToString(CultureInfo.InvariantCulture)}]); 
                    map.setZoom(14);";
                            
            return html.Replace("map.on('load', function () {", "map.on('load', function () {\n" + js);
        }

        public string GenerateUpdateLocationScript(Location location, double heading)
        {
            string speed = ((location.Speed ?? 0) * 3.6).ToString("F1", CultureInfo.InvariantCulture);
            return $@"window.updateUserLocation(
                {location.Longitude.ToString(CultureInfo.InvariantCulture)}, 
                {location.Latitude.ToString(CultureInfo.InvariantCulture)}, 
                {heading.ToString(CultureInfo.InvariantCulture)}, 
                {speed});";
        }

        public async Task<string> LoadHtmlAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            return html;
        }

        private bool IsActiveAtTime(string timeRange, TimeSpan current)
        {
            try
            {
                var parts = timeRange.Split(new[] { " do " }, StringSplitOptions.None);
                return TimeSpan.TryParse(parts[0], out var start) &&
                       TimeSpan.TryParse(parts[1], out var end) &&
                       current >= start && current <= end;
            }
            catch
            {
                return false;
            }
        }
    }
}