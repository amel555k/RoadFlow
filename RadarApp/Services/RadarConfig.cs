using System.Collections.Generic;

namespace RadarApp.Services
{
    public class RadarConfig
    {
        public static List<RadarLocation> Locations = new List<RadarLocation>
        {
            new RadarLocation
            {
                Name = "Travnik",
                PossibleIds = new List<int> { 417, 415 }
            },
            new RadarLocation
            {
                Name = "Vitez",
                PossibleIds = new List<int> { 400, 330 }
            },
            new RadarLocation
            {
                Name = "Zenica",
                PossibleIds = new List<int> { 323 }
            }
        };

        public static List<RadarCoordinate> Coordinates = new List<RadarCoordinate>
        {
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "ulica Erika Brandisa", "Erika Brandisa", "uliva Erika Brandisa" },
                Latitude = 44.224339,
                Longitude = 17.665682,
                SpeedLimit = 30
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Kalibunar (M-5)", "M-5 Kalibunar", "M-5 Kalibunat", "Kalibunar" },
                Latitude = 44.23165837901651,
                Longitude = 17.636192268618732,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Kalibunar (M-5)", "M-5 Kalibunar", "M-5 Kalibunat" },
                Latitude = 44.22878541002124,
                Longitude = 17.64796224813097,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Vrelo (M-5)", "M-5 Vrelo" },
                Latitude = 44.244515,
                Longitude = 17.594268,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Donje Putićevo (M-5)", "M-5 Donje Putićevo" },
                Latitude = 44.211025,
                Longitude = 17.709256,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Turbe (M-5)", "M-5 Turbe" },
                Latitude = 44.242521,
                Longitude = 17.586579,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Kanare", "Kanare (M-5)" },
                Latitude = 44.224728,
                Longitude = 17.684708,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Kanare", "Kanare (M-5)" },
                Latitude = 44.227329,
                Longitude = 17.680557,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Aleja Konzula", "ulica Aleja Konzula", "ul. Aleja Konzula" },
                Latitude = 44.226121,
                Longitude = 17.647507,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Dolac na Lašvi" },
                Latitude = 44.216944,
                Longitude = 17.694915,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "R441-Počulica", "R-441 Počulica", "R 441 Počulica" },
                Latitude = 44.167670,
                Longitude = 17.834732,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Divjak" },
                Latitude = 44.166250,
                Longitude = 17.770562,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Lokalna cesta Divjak" },
                Latitude = 44.164295,
                Longitude = 17.767976,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Dolac na Lašvi (škola)" },
                Latitude = 44.216514,
                Longitude = 17.690758,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Polje Slavka Gavranovića" },
                Latitude = 44.208375,
                Longitude = 17.695194,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "R-440 Bila", "R440-Bila" },
                Latitude = 44.187022,
                Longitude = 17.753603,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Nova Bila (škola)" },
                Latitude = 44.189062,
                Longitude = 17.739428,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Nova Bila" },
                Latitude = 44.193082,
                Longitude = 17.733456,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Mehurići (škola)", "Mehurići" },
                Latitude = 44.271682,
                Longitude = 17.734149,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Šantići" },
                Latitude = 44.146207,
                Longitude = 17.827426,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "ulica Lašvanska" },
                Latitude = 44.162307,
                Longitude = 17.788039,
                SpeedLimit = 30
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "ulica Lašvanska" },
                Latitude = 44.162223,
                Longitude = 17.791571,
                SpeedLimit = 30
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Ulica Branilaca Starog Viteza", "ulica Branilaca Starog Viteza" },
                Latitude = 44.158769,
                Longitude = 17.786026,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "ulica Kralja Tvrtka" },
                Latitude = 44.160276,
                Longitude = 17.783686,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Stjepana Radića", "ulica Stjepana Radića" },
                Latitude = 44.149454,
                Longitude = 17.804979,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Krčevine", "lokalna cesta PC 96 II, Krčevine" },
                Latitude = 44.165096,
                Longitude = 17.791257,
                SpeedLimit = 50 
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "R-441 Dubravica" },
                Latitude = 44.152590,
                Longitude = 17.813073,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "R-441 Dubravica" },
                Latitude = 44.150207,
                Longitude = 17.810891,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "R-440 Zabilje", "R 440 Zabilje" },
                Latitude = 44.195071,
                Longitude = 17.759600,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "ulica Hrvatske mladeži" },
                Latitude = 44.145020,
                Longitude = 17.794997,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Guča Gora" },
                Latitude = 44.243166,
                Longitude = 17.728671,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "Han Bila (škola)", "Han Bila (škoa)" },
                Latitude = 44.237260,
                Longitude = 17.758765,
                SpeedLimit = 40
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Jardol" },
                Latitude = 44.170353,
                Longitude = 17.776828,
                SpeedLimit = 60
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Ahmići" },
                Latitude = 44.143064,
                Longitude = 17.837669,
                SpeedLimit = 50
            },
            new RadarCoordinate
            {
                PossibleNames = new List<string> { "M-5 Bila" },
                Latitude = 44.180971,
                Longitude = 17.752092,
                SpeedLimit = 50
            }
        };
    }

    public class RadarLocation
    {
        public string Name { get; set; }
        public List<int> PossibleIds { get; set; }
    }

    public class RadarCoordinate
    {
        public List<string> PossibleNames { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SpeedLimit { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string MainName => PossibleNames.FirstOrDefault() ?? "Nepoznato";
    }
}