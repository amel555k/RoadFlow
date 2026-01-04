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

namespace RadarApp.Services
{
    public class RadarParser
    {
        private readonly HttpClient _httpClient;
        private readonly string _filePath;
        private const string BaseUrl = "***REMOVED***";

        public RadarParser()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "lista.txt");
        }

        // Glavna metoda - paralelno skida podatke sa svih linkova i vraća samo današnje radare
        public async Task<List<RadarData>> ParseAllLocationsAsync()
        {
            var rawRadars = new List<RadarData>();
            var todayDate = DateTime.Today;

            // Paralelno skidanje podataka sa svih ID-jeva
            var tasks = new List<Task<List<RadarData>>>();

            foreach (var location in RadarConfig.Locations)
            {
                foreach (var id in location.PossibleIds)
                {
                    tasks.Add(ParseSingleIdWithErrorHandlingAsync(location.Name, id));
                }
            }

            // Čeka da se svi taskovi završe odjednom
            var allResults = await Task.WhenAll(tasks);

            // Spaja rezultate i filtrira po današnjem datumu
            foreach (var radarsFromLink in allResults)
            {
                foreach (var radar in radarsFromLink)
                {
                    // Dodaje samo današnje radare ili info/error poruke
                    if ((radar.PageDate.HasValue && radar.PageDate.Value.Date == todayDate) ||
                        radar.Time == "INFO" || radar.Time == "GREŠKA")
                    {
                        rawRadars.Add(radar);
                    }
                }
            }

            // Sortira po datumu (najnoviji prvo)
            var finalRadars = rawRadars
                .OrderByDescending(r => r.PageDate ?? DateTime.MinValue)
                .ToList();

            // Ako ništa nije pronađeno, dodaje informativnu poruku
            if (!finalRadars.Any())
            {
                finalRadars.Add(new RadarData
                {
                    City = "STATUS SISTEMA",
                    Time = "INFO",
                    Location = $"Nisu pronađeni radari za današnji datum ({todayDate:dd.MM.yyyy}).",
                    PageDate = DateTime.Now
                });
            }

            // Snima rezultate u fajl
            var sb = new StringBuilder();
            var grouped = finalRadars.GroupBy(r => r.City);

            foreach (var group in grouped)
            {
                sb.AppendLine($"=== {group.Key} ===");
                foreach (var radar in group)
                {
                    sb.AppendLine($"{radar.Time} - {radar.Location}");
                }
                sb.AppendLine();
            }

            await SaveToFileAsync(sb.ToString());

            return finalRadars;
        }

        // Wrapper metoda sa error handlingom za pojedinačni ID
        private async Task<List<RadarData>> ParseSingleIdWithErrorHandlingAsync(string baseCityName, int id)
        {
            try
            {
                return await ParseSingleIdAsync(baseCityName, id);
            }
            catch (Exception ex)
            {
                // Vraća error poruku ako pukne (npr. nema interneta)
                return new List<RadarData>
                {
                    new RadarData
                    {
                        City = $"{baseCityName} - GREŠKA",
                        Time = "INFO",
                        Location = $"Greška: {ex.Message}",
                        PageDate = DateTime.Now
                    }
                };
            }
        }

        // Parsira jedan link i izvlači radare sa njega
        private async Task<List<RadarData>> ParseSingleIdAsync(string baseCityName, int id)
        {
            var radars = new List<RadarData>();
            string url = $"{BaseUrl}{id}";

            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var fullText = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
            
            // Čisti tekst od čudnih razmaka i karaktera
            fullText = fullText.Replace("\u00A0", " ");
            fullText = Regex.Replace(fullText, @"\s+", " ");

            DateTime? foundDate = ExtractDateFromHtml(doc, fullText);
            string cityName = baseCityName;

            // Regex za vrijeme: hvata "HH:mm do HH:mm" (sa ili bez "sati")
            var timePattern = @"\d{1,2}:\d{2}(?:\s*sati)?\s*[–\-do]+\s*\d{1,2}:\d{2}(?:\s*sati)?";
            var timeMatches = Regex.Matches(fullText, timePattern);

            if (timeMatches.Count > 0)
            {
                for (int i = 0; i < timeMatches.Count; i++)
                {
                    var timeMatch = timeMatches[i];
                    var rawTime = timeMatch.Value.Trim();

                    // Čisti vrijeme za prikaz (uklanja "sati")
                    var cleanTime = Regex.Replace(rawTime, @"\s*sati", "", RegexOptions.IgnoreCase);
                    cleanTime = cleanTime.Replace("–", " do ").Replace("-", " do ");
                    cleanTime = Regex.Replace(cleanTime, @"\s+", " ");

                    // Izvlači tekst između dva vremena (lokacija radara)
                    int startPos = timeMatch.Index + timeMatch.Length;
                    int endPos = (i < timeMatches.Count - 1) ? timeMatches[i + 1].Index : fullText.Length;

                    var locationPart = fullText.Substring(startPos, endPos - startPos).Trim();

                    // Dodatno čišćenje lokacije od neželjenog teksta
                    var googleIndex = locationPart.IndexOf("PRIKAŽI NA GOOGLE MAPI", StringComparison.OrdinalIgnoreCase);
                    if (googleIndex >= 0) locationPart = locationPart.Substring(0, googleIndex);

                    var zatvoriIndex = locationPart.IndexOf("Zatvori", StringComparison.OrdinalIgnoreCase);
                    if (zatvoriIndex >= 0) locationPart = locationPart.Substring(0, zatvoriIndex);

                    locationPart = locationPart.Trim();

                    if (!string.IsNullOrWhiteSpace(locationPart))
                    {
                        radars.Add(new RadarData
                        {
                            City = cityName,
                            Time = cleanTime,
                            Location = locationPart,
                            PageDate = foundDate ?? DateTime.MinValue
                        });
                    }
                }
            }
            else
            {
                // Ako nema vremena, znači nema radara za ovaj ID
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

        // Izvlači datum iz HTML-a (prvo traži u naslovu, zatim u tekstu)
        private DateTime? ExtractDateFromHtml(HtmlDocument doc, string fullText)
        {
            // Pokušava naći datum u naslovu
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

            // Ako nije našao u naslovu, traži ZADNJI datum u tekstu (obično najrelevantiji)
            var regex = new Regex(@"([0-9]{1,2})\.\s*([0-9]{1,2})\.\s*([0-9]{4})\.?");
            var matches = regex.Matches(fullText);

            if (matches.Count > 0)
            {
                var match = matches[matches.Count - 1];

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

            return null;
        }

        // Izvlači datum iz teksta pomoću regex-a
        private DateTime? ExtractDateFromText(string text)
        {
            var regex = new Regex(@"([0-9]{1,2})\.\s*([0-9]{1,2})\.\s*([0-9]{4})\.?");
            var match = regex.Match(text);

            if (match.Success)
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
            return null;
        }

        // Snima sadržaj u fajl
        private async Task SaveToFileAsync(string content)
        {
            try
            {
                await File.WriteAllTextAsync(_filePath, content, Encoding.UTF8);
            }
            catch { }
        }

        // Čita sadržaj iz fajla
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

        // Vraća sve radare iz fajla (bez vremenskog filtriranja)
        public async Task<List<RadarData>> GetActiveRadarsAsync()
        {
            var fileContent = await ReadFromFileAsync();
            if (string.IsNullOrWhiteSpace(fileContent))
                return new List<RadarData>();

            var allRadars = ParseFileContent(fileContent);

            return allRadars;
        }

        // Parsira sadržaj fajla i pretvara ga u listu RadarData objekata
        private List<RadarData> ParseFileContent(string content)
        {
            var radars = new List<RadarData>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string currentCity = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Ako je header sa imenom grada
                if (trimmed.StartsWith("===") && trimmed.EndsWith("==="))
                {
                    currentCity = trimmed.Replace("===", "").Trim();
                    continue;
                }

                // Parsira liniju formata "vrijeme - lokacija"
                var parts = trimmed.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    radars.Add(new RadarData
                    {
                        City = currentCity,
                        Time = parts[0].Trim(),
                        Location = parts[1].Trim(),
                        PageDate = DateTime.Now
                    });
                }
            }
            return radars;
        }
    }

    public class RadarData
    {
        public string City { get; set; }
        public string Time { get; set; }
        public string Location { get; set; }
        public DateTime? PageDate { get; set; }

        // Provjerava da li je radar aktivan za dato vrijeme
        public bool IsActiveAt(TimeSpan currentTime)
        {
            if (Time == "INFO" || Time == "GREŠKA") return true;
            try
            {
                var parts = Time.Split(new[] { " do " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) return false;

                if (!TimeSpan.TryParse(parts[0].Trim(), out TimeSpan startTime)) return false;
                if (!TimeSpan.TryParse(parts[1].Trim(), out TimeSpan endTime)) return false;

                return currentTime >= startTime && currentTime <= endTime;
            }
            catch { return false; }
        }
    }
}