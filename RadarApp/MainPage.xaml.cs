using RadarApp.Services;
using System.Collections.Generic;
using System.Text;

namespace RadarApp
{
    public partial class MainPage : ContentPage
    {
        private readonly RadarParser _parser;
        private List<RadarData> _currentRadars;
        private bool _isMenuOpen = false;

        // Konstruktor - inicijalizuje komponentu, parser, postavlja datum i učitava podatke
        public MainPage()
        {
            InitializeComponent();
            _parser = new RadarParser();
            LblDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            _ = LoadRadarDataAsync();
        }

        // Toggle funkcija za burger meni - otvara ili zatvara meni
        private async void OnBurgerMenuClicked(object sender, EventArgs e)
        {
            if (_isMenuOpen)
            {
                await CloseMenu();
            }
            else
            {
                await OpenMenu();
            }
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

        // Prebacuje na Lista view i učitava podatke
        private async void OnListaClicked(object sender, EventArgs e)
        {
            await CloseMenu();
            ContentFrame.IsVisible = true;

            // Mijenja boje dugmadi da prikaže aktivnu opciju
            BtnLista.BackgroundColor = Color.FromArgb("#3498DB");
            BtnMapa.BackgroundColor = Color.FromArgb("#95A5A6");

            await LoadRadarDataAsync();
        }

        // Prebacuje na Mapa view
        private async void OnMapaClicked(object sender, EventArgs e)
        {
            await CloseMenu();

            // Mijenja boje dugmadi da prikaže aktivnu opciju
            BtnMapa.BackgroundColor = Color.FromArgb("#3498DB");
            BtnLista.BackgroundColor = Color.FromArgb("#95A5A6");

            await Shell.Current.GoToAsync("///map");
        }

        // Handler za refresh dugme - ponovo učitava podatke
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadRadarDataAsync();
        }

        // Prikazuje putanju do lista.txt fajla sa dodatnim informacijama
        private async void OnShowPathClicked(object sender, EventArgs e)
        {
            var filePath = Path.Combine(FileSystem.AppDataDirectory, "lista.txt");

            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;
                var lastModified = fileInfo.LastWriteTime;

                await DisplayAlert("Lokacija fajla",
                    $"Putanja:\n{filePath}\n\nVeličina: {fileSize} bytes\nZadnja izmjena: {lastModified:dd.MM.yyyy HH:mm:ss}",
                    "OK");
            }
            else
            {
                await DisplayAlert("Lokacija fajla",
                    $"Fajl još ne postoji.\nBit će kreiran na:\n{filePath}", "OK");
            }
        }

        // Asinhrono učitava radar podatke sa loading indikatorom
        private async Task LoadRadarDataAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                BtnRefresh.IsEnabled = false;

                _currentRadars = await _parser.ParseAllLocationsAsync();
                DisplayRadarData(_currentRadars);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Došlo je do greške: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                BtnRefresh.IsEnabled = true;
            }
        }

        // Prikazuje radar podatke grupisane po gradovima
        private void DisplayRadarData(List<RadarData> radars)
        {
            RadarListContainer.Children.Clear();

            if (radars == null || radars.Count == 0)
            {
                RadarListContainer.Children.Add(new Label
                {
                    Text = "Nema dostupnih podataka",
                    FontSize = 16,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            // Grupira radare po gradovima
            var groupedRadars = radars.GroupBy(r => r.City);

            foreach (var group in groupedRadars)
            {
                // Header frame sa nazivom grada
                var cityFrame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#3498DB"),
                    Padding = new Thickness(10, 5),
                    Margin = new Thickness(0, 5, 0, 5),
                    CornerRadius = 3
                };

                var cityLabel = new Label
                {
                    Text = group.Key,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                };

                cityFrame.Content = cityLabel;
                RadarListContainer.Children.Add(cityFrame);

                // Dodaje svaki radar u grupi
                foreach (var radar in group)
                {
                    var radarFrame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#ECF0F1"),
                        Padding = new Thickness(10),
                        Margin = new Thickness(5, 2, 5, 2),
                        CornerRadius = 5,
                        HasShadow = false
                    };

                    var radarStack = new HorizontalStackLayout { Spacing = 10 };

                    // Label za vrijeme
                    var timeLabel = new Label
                    {
                        Text = radar.Time,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#2C3E50"),
                        VerticalOptions = LayoutOptions.Center,
                        WidthRequest = 80
                    };

                    // Label za lokaciju
                    var locationLabel = new Label
                    {
                        Text = radar.Location,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#34495E"),
                        VerticalOptions = LayoutOptions.Center,
                        LineBreakMode = LineBreakMode.WordWrap
                    };

                    radarStack.Children.Add(timeLabel);
                    radarStack.Children.Add(locationLabel);

                    radarFrame.Content = radarStack;
                    RadarListContainer.Children.Add(radarFrame);
                }
            }
        }
    }
}