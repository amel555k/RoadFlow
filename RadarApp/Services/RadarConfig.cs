using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RadarApp.Models;

namespace RadarApp.Services
{
    public class RadarConfig
    {
        public static List<RadarLocation> Locations = new List<RadarLocation>
        {
            new RadarLocation
            {
                Name = "Travnik",
                PossibleIds = new List<int> { 417, 415 },
                Canton=Canton.Srednjobosanski,
                MapEnabled=true,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Vitez",
                PossibleIds = new List<int> { 400, 330 },
                Canton=Canton.Srednjobosanski,
                MapEnabled=true,
                FromFirebase=true
            },
            
            new RadarLocation
            {
                Name = "Fojnica",
                PossibleIds = new List<int> { 418, 360 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Donji Vakuf",
                PossibleIds = new List<int> { 419, 416 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            }, 
            new RadarLocation
            {
                Name = "Bugojno",
                PossibleIds = new List<int> { 402, 332 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Jajce",
                PossibleIds = new List<int> { 335, 404 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Gornji Vakuf-Uskoplje",
                PossibleIds = new List<int> { 333, 403 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Busovača",
                PossibleIds = new List<int> { 336, 405 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Kiseljak",
                PossibleIds = new List<int> { 334, 420 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name = "Novi Travnik",
                PossibleIds = new List<int> { 401, 331 },
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
            new RadarLocation
            {
                Name="Kreševo",
                PossibleIds=new List<int>{697},
                Canton=Canton.Srednjobosanski,
                FromFirebase=true
            },
             new RadarLocation
            {
                Name = "Zenica",
                PossibleIds = new List<int> { 323,393 },
                Canton=Canton.ZenickoDobojski,
                MapEnabled=true
            },
             new RadarLocation
            {
                Name = "Tešanj",
                PossibleIds = new List<int> { 328,399 },
                Canton=Canton.ZenickoDobojski,
            }
            ,
             new RadarLocation
            {
                Name = "Maglaj",
                PossibleIds = new List<int> { 327,397 },
                Canton=Canton.ZenickoDobojski,
            },
             new RadarLocation
            {
                Name = "Doboj Jug",
                PossibleIds = new List<int> { 354,353 },
                Canton=Canton.ZenickoDobojski,
            },
             new RadarLocation
            {
                Name = "Žepče",
                PossibleIds = new List<int> { 453 },
                Canton=Canton.ZenickoDobojski,
            },
            new RadarLocation
            {
                Name = "Tuzla",
                PossibleIds = new List<int> { 391,319 },
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Gračanica",
                PossibleIds = new List<int> { 471, 355 },
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Kalesija",
                PossibleIds = new List<int> { 2059, 3987 },
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Gradačac",
                PossibleIds = new List<int> { 388 },
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Čelić",
                PossibleIds = new List<int> { 392, 320},
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Srebrenik",
                PossibleIds = new List<int> { 2971, 2831 },
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Banovići",
                PossibleIds = new List<int> { 435, 806 },
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Sapna",
                PossibleIds = new List<int> { 721,1551},
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Teočak",
                PossibleIds = new List<int> { 745,3242},
                Canton=Canton.Tuzlanski,
            },
            new RadarLocation
            {
                Name = "Sarajevo",
                PossibleIds = new List<int> { 342,412 },
                Canton=Canton.Sarajevo,
            },
            new RadarLocation
            {
                Name = "Brčko",
                PossibleIds = new List<int> { 4821,4822 },
                Canton=Canton.BrckoDistrikt,
            },
            
        

        };
        public static List<RadarCoordinate> Coordinates = new List<RadarCoordinate>
        {
            new RadarCoordinate
            {
                MainName = "ulica Erika Brandisa",
                Latitude = 44.224339,
                Longitude = 17.665682,
                SpeedLimit = 30
            },
            new RadarCoordinate
            {
                MainName = "Kalibunar (M-5)",
                Latitude = 44.23165837901651,
                Longitude = 17.636192268618732,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Kalibunar (M-5)",
                Latitude = 44.22878541002124,
                Longitude = 17.64796224813097,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Vrelo (M-5)",
                Latitude = 44.244515,
                Longitude = 17.594268,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Karaulska cesta",
                Latitude = 44.245832,
                Longitude = 17.574472,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Donje Putićevo (M-5)",
                Latitude = 44.211025,
                Longitude = 17.709256,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                MainName = "Turbe (M-5)",
                Latitude = 44.242521,
                Longitude = 17.586579,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "M-5 Kanare",
                Latitude = 44.224728,
                Longitude = 17.684708,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                MainName = "M-5 Kanare",
                Latitude = 44.227329,
                Longitude = 17.680557,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                MainName = "Aleja Konzula",
                Latitude = 44.226121,
                Longitude = 17.647507,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "Dolac na Lašvi",
                Latitude = 44.216944,
                Longitude = 17.694915,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "R441-Počulica",
                Latitude = 44.167670,
                Longitude = 17.834732,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "M-5 Divjak",
                Latitude = 44.166250,
                Longitude = 17.770562,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Lokalna cesta Divjak",
                Latitude = 44.164295,
                Longitude = 17.767976,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Dolac na Lašvi (škola)",
                Latitude = 44.216514,
                Longitude = 17.690758,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "Polje Slavka Gavrančića",
                Latitude = 44.208375,
                Longitude = 17.695194,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "R-440 Bila",
                Latitude = 44.187022,
                Longitude = 17.753603,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Nova Bila (škola)",
                Latitude = 44.189062,
                Longitude = 17.739428,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "Nova Bila (škola)",
                Latitude = 44.189991,
                Longitude = 17.737367,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "M-5 Nova Bila",
                Latitude = 44.193082,
                Longitude = 17.733456,
                SpeedLimit = 50
            },
             new RadarCoordinate
            {
                MainName = "Nova Bila",
                Latitude = 44.193082,
                Longitude = 17.733456,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Mehurići (škola)",
                Latitude = 44.271682,
                Longitude = 17.734149,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Mosor",
                Latitude = 44.233509,
                Longitude = 17.711371,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "M-5 Šantići",
                Latitude = 44.146207,
                Longitude = 17.827426,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                MainName = "ulica Lašvanska",
                Latitude = 44.162307,
                Longitude = 17.788039,
                SpeedLimit = 30
            },
            new RadarCoordinate
            {
                MainName = "ulica Lašvanska",
                Latitude = 44.162223,
                Longitude = 17.791571,
                SpeedLimit = 30
            },
            new RadarCoordinate
            {
                MainName = "Ulica Branilaca Starog Viteza",
                Latitude = 44.158769,
                Longitude = 17.786026,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "ulica Kralja Tvrtka",
                Latitude = 44.160276,
                Longitude = 17.783686,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "Stjepana Radića",
                Latitude = 44.149454,
                Longitude = 17.804979,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "M-5 Krčevine",
                Latitude = 44.165096,
                Longitude = 17.791257,
                SpeedLimit = 50 
            },
            new RadarCoordinate
            {
                MainName = "lokalna cesta PC 96 II, Krčevine",
                Latitude = 44.165240,
                Longitude = 17.790378,
                SpeedLimit = 50 
            },
            new RadarCoordinate
            {
                MainName = "lokalna cesta PC 96 II, Krčevine",
                Latitude = 44.167380,
                Longitude = 17.786715,
                SpeedLimit = 50 
            },
            new RadarCoordinate
            {
                MainName = "R-441 Dubravica",
                Latitude = 44.152590,
                Longitude = 17.813073,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "R-441 Dubravica",
                Latitude = 44.150207,
                Longitude = 17.810891,
                SpeedLimit = 50
            }, 
            new RadarCoordinate
            {
                MainName = "R-441 Zabilje",
                Latitude = 44.195071,
                Longitude = 17.759600,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "ulica Hrvatske mladeži",
                Latitude = 44.145020,
                Longitude = 17.794997,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Guča Gora",
                Latitude = 44.243166,
                Longitude = 17.728671,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "Han Bila (škola)",
                Latitude = 44.237260,
                Longitude = 17.758765,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                MainName = "M-5 Jardol",
                Latitude = 44.170353,
                Longitude = 17.776828,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                MainName = "M-5 Ahmići",
                Latitude = 44.143064,
                Longitude = 17.837669,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                MainName = "M-5 Bila",
                Latitude = 44.180971,
                Longitude = 17.752092,
                SpeedLimit = 50
            }
        };
        public static List<RadarCoordinate> FindCoordinatesByName(string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName))
                return new List<RadarCoordinate>();

            var mainName = ResolveMainName(locationName);
            if (mainName == null)
                return new List<RadarCoordinate>();
            return Coordinates.Where(c => c.MainName == mainName).ToList();
        }

        public static RadarCoordinate FindCoordinateByName(string locationName)
        {
            return FindCoordinatesByName(locationName).FirstOrDefault();
        }

        private static string ResolveMainName(string raw)
        {
           var n = StripDiacritics(raw.ToLowerInvariant().Trim());
    
            
            if (Contains(n, "lok") && Contains(n, "divjak"))
                return "Lokalna cesta Divjak";
            
            if (Contains(n, "lok") && (Contains(n, "pc 96") || Contains(n, "pc96") || Contains(n, "krcevine")))
                return "lokalna cesta PC 96 II, Krčevine";

            n = RemoveStreetPrefixes(n);

            if (Contains(n, "bolnici") || Contains(n, "prema bolnici"))
                return "R-440 Bila";

            if ((Contains(n, "r-440") || Contains(n, "r440") ||
                 Contains(n, "r-441") || Contains(n, "r441")) && Contains(n, "bila"))
                return "R-440 Bila";

            if (Contains(n, "han bila"))
                return "Han Bila (škola)";

            if (Contains(n, "nova bila") &&
                (Contains(n, "m-5") || Contains(n, "m5") || Contains(n, "m 5")))
                return "M-5 Nova Bila";

            if (Contains(n, "nova bila") && (Contains(n, "skola") || Contains(n, "skole")))
                return "Nova Bila (škola)";

            if (Contains(n, "nova bila"))
                return "Nova Bila";

            if (Contains(n, "bila") &&
                (Contains(n, "m-5") || Contains(n, "m5") || Contains(n, "m 5")))
                return "M-5 Bila";

            if (n == "bila")
                return "M-5 Bila";

            if (Contains(n, "krcevine") &&
                (Contains(n, "m-5") || Contains(n, "m5") || Contains(n, "m 5")))
                return "M-5 Krčevine";

            if (Contains(n, "krcevine"))
                return "M-5 Krčevine";

            if (Contains(n, "kalibunar"))
                return "Kalibunar (M-5)";

            if (Contains(n, "vrelo"))
                return "Vrelo (M-5)";
                
            if (Contains(n, "karaulska"))
                return "Karaulska cesta";

            if (Contains(n, "turbe"))
                return "Turbe (M-5)";

            if (Contains(n, "puticevo") || Contains(n, "putićevo"))
                return "Donje Putićevo (M-5)";

            if (Contains(n, "dolac") && Contains(n, "lasvi") && (Contains(n, "skola") || Contains(n, "skole")))
                return "Dolac na Lašvi (škola)";
            if (Contains(n, "dolac") && Contains(n, "lasvi"))
                return "Dolac na Lašvi";

            if (Contains(n, "mehuric"))
                return "Mehurići (škola)";

            if (Contains(n, "dubravica"))
                return "R-441 Dubravica";

            if (Contains(n, "dubravice"))
                return "R-441 Dubravica";

            if (Contains(n, "zabilje"))
                return "R-441 Zabilje";

            if (Contains(n, "poculica") || Contains(n, "počulica"))
                return "R441-Počulica";

            if (Contains(n, "erika brandisa") || Contains(n, "erika brandis"))
                return "ulica Erika Brandisa";

            if (Contains(n, "lasvanska") || Contains(n, "lašvanska"))
                return "ulica Lašvanska";

            if (Contains(n, "branilaca starog viteza") || Contains(n, "branilaca st. viteza"))
                return "Ulica Branilaca Starog Viteza";

            if (Contains(n, "kralja tvrtka"))
                return "ulica Kralja Tvrtka";

            if (Contains(n, "hrvatske mladezi") || Contains(n, "hrvatske mlade"))
                return "ulica Hrvatske mladeži";

            if (Contains(n, "aleja konzula") || Contains(n, "konzula"))
                return "Aleja Konzula";

            if (Contains(n, "stjepana radica") || Contains(n, "stjepana radic") ||
                (Contains(n, "radica") && !Contains(n, "branilaca")))
                return "Stjepana Radića";

            if (Contains(n, "kanare"))
                return "M-5 Kanare";

            if (Contains(n, "santici") || Contains(n, "šantići"))
                return "M-5 Šantići";

            if (Contains(n, "jardol"))
                return "M-5 Jardol";

            if (Contains(n, "ahmic") || Contains(n, "ahmić"))
                return "M-5 Ahmići";

            if (Contains(n, "divjak"))
            return "M-5 Divjak";

            if (Contains(n, "polje") && (Contains(n, "slavka") || Contains(n, "gavrancica") || Contains(n, "gavrančića")))
                return "Polje Slavka Gavrančića";

            if (Contains(n, "slavka") || Contains(n, "gavrancica") || Contains(n, "gavrančića"))
                return "Polje Slavka Gavrančića";

            if (Contains(n, "guca gora") || Contains(n, "guča gora"))
                return "Guča Gora";

            if (Contains(n, "mosor"))
                return "Mosor";

            System.Diagnostics.Debug.WriteLine($"[RadarConfig] NIJE PREPOZNATO: '{raw}'");
            return null;
        }

        private static bool Contains(string source, string value)
            => source.Contains(value, System.StringComparison.Ordinal);

        private static string RemoveStreetPrefixes(string n)
        {
            var prefixes = new[]
            {
                "ulica ", "ul. ", "ul ", "ulica.", "lokalna cesta ", "lok. cesta ",
                "aleja ", "cesta "
            };
            foreach (var p in prefixes)
                if (n.StartsWith(p)) return n.Substring(p.Length).TrimStart();
            return n;
        }

        private static string StripDiacritics(string text)
        {
            return text
                .Replace('č', 'c').Replace('ć', 'c')
                .Replace('š', 's')
                .Replace('ž', 'z')
                .Replace('đ', 'd')
                .Replace('Č', 'C').Replace('Ć', 'C')
                .Replace('Š', 'S')
                .Replace('Ž', 'Z')
                .Replace('Đ', 'D');
        }
    }
}