using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Maui.Storage;
using RoadFlow.Models;

namespace RoadFlow.Services
{
    public class RadarParser
    {
        private readonly HttpClient _httpClient;
        private readonly FirebaseService _firebaseService=new FirebaseService();
        private readonly string _filePath;
        private const string BaseUrl = Secrets.URL;

        private readonly Dictionary<int, Task<string>> _htmlCache = new Dictionary<int, Task<string>>();

        public RadarParser()
        {
            _httpClient = new HttpClient();
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "lista.txt");
        }

        public async Task<List<RadarData>> ParseAllLocationsAsync()
        {
            var todayDate = DateTime.Today;
            
            if (File.Exists(_filePath))
            {
                var lastWriteTime = File.GetLastWriteTime(_filePath);
                if (lastWriteTime.Date == todayDate)
                {
                    System.Diagnostics.Debug.WriteLine("Učitavam radare iz lokalnog keša (lista.txt)...");
                    var cachedContent = await ReadFromFileAsync();
                    return ParseFileContent(cachedContent);
                }
            }
            var rawRadars = new List<RadarData>();
            
            var firebaseData= await _firebaseService.GetFirebaseRadarsAsync(todayDate);
            rawRadars.AddRange(firebaseData);

            lock (_htmlCache) { _htmlCache.Clear(); }

            if (Microsoft.Maui.Networking.Connectivity.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
            {
                var noConnectionResult = new List<RadarData>
                {
                    new RadarData
                    {
                        City = "STATUS SISTEMA",
                        Time = "INFO",
                        Location = "Molimo provjerite internet konekciju.",
                        PageDate = DateTime.Now
                    }
                };
                return noConnectionResult;
            }

            var taskTuples = new List<(string CityName, int Id, Task<List<RadarData>> Task)>();

            foreach (var location in RadarConfig.Locations)
            {
                if(location.FromFirebase) continue;
                
                foreach (var id in location.PossibleIds)
                {
                    taskTuples.Add((location.Name, id, ParseSingleIdWithErrorHandlingAsync(location.Name, id, location.MapEnabled)));
                }
            }

            await Task.WhenAll(taskTuples.Select(t => t.Task));

            foreach (var (cityName, id, task) in taskTuples)
            {
                var radarsFromLink = task.Result;

                foreach (var radar in radarsFromLink)
                {
                    if ((radar.PageDate.HasValue && radar.PageDate.Value.Date == todayDate) ||
                        radar.Time == "INFO")
                    {
                        rawRadars.Add(radar);
                    }
                }
            }

            var finalRadars = rawRadars
            .Where(r => r.Time != "INFO")
            .GroupBy(r => new { r.City, r.Time, r.Location })
            .Select(g => g.First())
            .OrderByDescending(r=>r.City)
            .ThenByDescending(r => r.PageDate ?? DateTime.MinValue)
            .ToList();

            if (!finalRadars.Any(r => r.Time != "INFO"))
            {
                finalRadars.Clear(); 
                
                finalRadars.Add(new RadarData
                {
                    City = "STATUS SISTEMA",
                    Time = "INFO",
                    Location = $"Nisu pronađeni radari za današnji datum ({todayDate:dd.MM.yyyy}).",
                    PageDate = DateTime.Now
                });
            }

          var uniqueRadars = finalRadars
                .GroupBy(r => new { r.City, r.Time, r.Location })
                .Select(g => g.First())
                .ToList();

            var sb = new StringBuilder();
            var groupedByCity = uniqueRadars.GroupBy(r => r.City);

            foreach (var cityGroup in groupedByCity)
            {
                sb.AppendLine($"=== {cityGroup.Key} ===");
                foreach (var radar in cityGroup)
                {
                    sb.AppendLine($"{radar.Time} - {radar.Location}");
                }
                sb.AppendLine();
            }

            await SaveToFileAsync(sb.ToString());

            return uniqueRadars;
        }


            private async Task<List<RadarData>> ParseSingleIdWithErrorHandlingAsync(string baseCityName, int id, bool mapEnabled)
            {
                try
                {
                    return await ParseSingleIdAsync(baseCityName, id, mapEnabled);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Greška pri parsiranju ID-a {id}: {ex.Message}");
                    return new List<RadarData>(); 
                }
            }
        
        private async Task<List<RadarData>> ParseSingleIdAsync(string baseCityName, int id, bool mapEnabled)
        {
            var radars = new List<RadarData>();
            string url = $"{BaseUrl}{id}";
            Task<string> htmlTask;
            lock (_htmlCache)
            {
                if (!_htmlCache.TryGetValue(id, out htmlTask))
                {
                    htmlTask = _httpClient.GetStringAsync(url);
                    _htmlCache[id] = htmlTask;
                    System.Diagnostics.Debug.WriteLine($"[Cache MISS] Fetching ID={id}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Cache HIT]  Reusing ID={id} za grad '{baseCityName}'");
                }
            }
            var html = await htmlTask;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var fullText = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
            fullText = fullText.Replace("\u00A0", " ");
            fullText = Regex.Replace(fullText, @"\s+", " ");
            if (id != 323 && id!=393 && !CityNameExistsInHtml(fullText, baseCityName))
            {
                System.Diagnostics.Debug.WriteLine($"Grad '{baseCityName}' se ne pojavljuje na stranici ID={id}. Preskačem.");
                return new List<RadarData>();
            }

            DateTime? foundDate = ExtractDateFromHtml(doc, fullText);
            string cityName = baseCityName;
            var timePattern = @"\d{1,2}:\d{2}(?:\s*sati)?\s*[–\-do]+\s*\d{1,2}:\d{2}(?:\s*sati)?";
            var timeMatches = Regex.Matches(fullText, timePattern);

            if (timeMatches.Count > 0)
            {
                for (int i = 0; i < timeMatches.Count; i++)
                {
                    var timeMatch = timeMatches[i];
                    var rawTime = timeMatch.Value.Trim();

                    var cleanTime = Regex.Replace(rawTime, @"\s*sati", "", RegexOptions.IgnoreCase);
                    cleanTime = cleanTime.Replace("–", " do ").Replace("-", " do ");
                    cleanTime = Regex.Replace(cleanTime, @"\s+", " ");

                    int startPos = timeMatch.Index + timeMatch.Length;
                    int endPos = (i < timeMatches.Count - 1) ? timeMatches[i + 1].Index : fullText.Length;

                    var locationPart = fullText.Substring(startPos, endPos - startPos).Trim();
                    char[] separators = { '-', ':', ' ', ',', '.', '–', '—' }; 
                    locationPart = locationPart.TrimStart(separators).Trim();

                    var googleIndex = locationPart.IndexOf("PRIKAŽI NA GOOGLE MAPI", StringComparison.OrdinalIgnoreCase);
                    if (googleIndex >= 0) locationPart = locationPart.Substring(0, googleIndex);

                    var zatvoriIndex = locationPart.IndexOf("Zatvori", StringComparison.OrdinalIgnoreCase);
                    if (zatvoriIndex >= 0) locationPart = locationPart.Substring(0, zatvoriIndex);

                    locationPart = locationPart.Trim();

                   
                    if (!string.IsNullOrWhiteSpace(locationPart))
                    {
                        locationPart = PreprocessBihamkLocation(locationPart);

                        if (mapEnabled)
                        {
                            var coords = RadarConfig.FindCoordinatesByName(locationPart);
                            if (coords.Any())
                            {
                                foreach (var coordinate in coords)
                                {
                                    radars.Add(new RadarData
                                    {
                                        City = cityName,
                                        Time = cleanTime,
                                        Location = locationPart,
                                        PageDate = foundDate,
                                        Coordinate = coordinate,
                                        Latitude = coordinate.Latitude,
                                        Longitude = coordinate.Longitude,
                                        SpeedLimit = coordinate.SpeedLimit
                                    });
                                }
                            }
                            else
                            {
                               
                                radars.Add(new RadarData
                                {
                                    City = cityName,
                                    Time = cleanTime,
                                    Location = locationPart,
                                    PageDate = foundDate
                                });
                            }
                        }
                        else
                        {
                            radars.Add(new RadarData
                            {
                                City = cityName,
                                Time = cleanTime,
                                Location = locationPart,
                                PageDate = foundDate
                            });
                        }
                    }

                }
            }
            else
            {
                radars.Add(new RadarData
                {
                    City = cityName,
                    Time = "INFO",
                    Location = "Nema planiranih radara za ovaj ID.",
                    PageDate = foundDate ?? DateTime.MinValue
                });
            }

            return radars;
        }

        private string PreprocessBihamkLocation(string location)
        {
                if (string.IsNullOrWhiteSpace(location))
                    return location;

                location = Regex.Replace(location, @"\s*sati\s*", " ", RegexOptions.IgnoreCase);
                
                location = Regex.Replace(location, @"\s+", " ");
                
                return location.Trim();
        } 

        private DateTime? ExtractDateFromHtml(HtmlDocument doc, string fullText)
        {
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1")
                            ?? doc.DocumentNode.SelectSingleNode("//h2")
                            ?? doc.DocumentNode.SelectSingleNode("//title");

            if (titleNode != null)
            {
                var titleText = System.Net.WebUtility.HtmlDecode(titleNode.InnerText);
                var dateFromTitle = ExtractDateFromText(titleText);
                if (dateFromTitle.HasValue)
                {
                    return dateFromTitle;
                }
            }
            var regex = new Regex(@"([0-9]{1,2})[\.\s]+([0-9]{1,2})[\.\s]+([0-9]{4})");
            var matches = regex.Matches(fullText);

            if (matches.Count > 0)
            {
                var match = matches[matches.Count - 1];
                return TryParseDateTime(match);
            }

            return null;
        }

        private DateTime? ExtractDateFromText(string text)
        {
            var regex = new Regex(@"([0-9]{1,2})[\.\s]+([0-9]{1,2})[\.\s]+([0-9]{4})");
            var match = regex.Match(text);

            if (match.Success)
            {
                return TryParseDateTime(match);
            }
            return null;
        }
        private DateTime? TryParseDateTime(Match match)
        {
            try
            {
                int day = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);
                int year = int.Parse(match.Groups[3].Value);
                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }
        private async Task SaveToFileAsync(string content)
        {
            try
            {
                await File.WriteAllTextAsync(_filePath, content, Encoding.UTF8);
                File.SetLastWriteTime(_filePath, DateTime.Now);
                
                System.Diagnostics.Debug.WriteLine($"Podaci sačuvani u {_filePath}");
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"Greška pri čuvanju fajla: {ex.Message}");
            }
}

        public async Task<string> ReadFromFileAsync()
        {
            try
            {
                if (File.Exists(_filePath))
                    return await File.ReadAllTextAsync(_filePath, Encoding.UTF8);
            }
            catch { }
            return string.Empty;
        }
        public async Task<List<RadarData>> GetActiveRadarsAsync()
        {
            var fileContent = await ReadFromFileAsync();
            if (string.IsNullOrWhiteSpace(fileContent))
                return new List<RadarData>();

            var allRadars = ParseFileContent(fileContent);

            return allRadars;
        }

        private List<RadarData> ParseFileContent(string content)
        {
            var radars = new List<RadarData>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string currentCity = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("===") && trimmed.EndsWith("==="))
                {
                    currentCity = trimmed.Replace("===", "").Trim();
                    continue;
                }

                var parts = trimmed.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var timePart = parts[0].Trim();
                    var locationName = parts[1].Trim();
                    var coordinates = RadarConfig.FindCoordinatesByName(locationName);

                    var radar = new RadarData
                    {
                        City = currentCity,
                        Time = timePart,
                        Location = locationName,
                        PageDate = DateTime.Now
                    };

                    if (coordinates.Any())
                    {
                        var first = coordinates.First();
                        radar.Coordinate = first;
                        radar.Latitude = first.Latitude;
                        radar.Longitude = first.Longitude;
                        radar.SpeedLimit = first.SpeedLimit;
                        
                        radar.AllCoordinates = coordinates; 
                    }

                    radars.Add(radar);
                }
            }

            return radars
                .GroupBy(r => new { r.City, r.Time, r.Location })
                .Select(g => g.First())
                .ToList();
        }
        private bool CityNameExistsInHtml(string htmlText, string cityName)
        {
            if (string.IsNullOrWhiteSpace(htmlText) || string.IsNullOrWhiteSpace(cityName))
                return false;

            var normalizedHtml = StripDiacriticsLocal(htmlText.ToLowerInvariant());
            var normalizedCity = StripDiacriticsLocal(cityName.ToLowerInvariant());

            if (normalizedHtml.Contains(normalizedCity))
                return true;

            var withDash = normalizedCity.Replace(" ", "-");
            if (normalizedHtml.Contains(withDash))
                return true;
            var noSpace = normalizedCity.Replace(" ", "");
            if (normalizedHtml.Contains(noSpace))
                return true;

            return false;
        }

        private static string StripDiacriticsLocal(string text)
        {
            return text
                .Replace('č', 'c').Replace('ć', 'c')
                .Replace('š', 's')
                .Replace('ž', 'z')
                .Replace('đ', 'd');
        }
    }
}