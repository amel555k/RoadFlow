using RadarApp.Models;
using RadarApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Devices.Sensors;

namespace RadarApp
{
    public class RadarFlatItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HeaderTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item is RadarFlatItem flatItem && flatItem.IsHeader
                ? HeaderTemplate
                : ItemTemplate;
        }
    }

    public partial class MainPage : ContentPage
    {
        public static readonly DataTemplateSelector RadarFlatItemTemplate = new RadarFlatItemTemplateSelector
        {
            HeaderTemplate = new DataTemplate(() =>
            {
                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#212143"),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 5, 0, 0),
                    CornerRadius = 10,
                    WidthRequest = 150,
                    HorizontalOptions = LayoutOptions.Start
                };
                var label = new Label
                {
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.White
                };
                label.SetBinding(Label.TextProperty, nameof(RadarFlatItem.CityName));
                frame.Content = label;
                return frame;
            }),
            ItemTemplate = new DataTemplate(() =>
{
    var outer = new Border
    {
        Stroke = Colors.Transparent,
        StrokeThickness = 0,
        BackgroundColor = Color.FromArgb("#D9D9D9"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
        Padding = new Thickness(8)
    };
    outer.SetBinding(Border.MarginProperty, nameof(RadarFlatItem.ItemMargin));

    var inner = new Border
    {
        Stroke = Colors.Transparent,
        StrokeThickness = 0,
        BackgroundColor = Colors.White,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
        Padding = new Thickness(10, 5)
    };

    var grid = new Grid { ColumnSpacing = 10 };
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 70 });
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

    var timeLabel = new Label
    {
        FontSize = 14,
        FontAttributes = FontAttributes.Bold,
        TextColor = Colors.Black,
        VerticalOptions = LayoutOptions.Center
    };
    timeLabel.SetBinding(Label.TextProperty, nameof(RadarFlatItem.Time));

    var locationLabel = new Label
    {
        FontSize = 14,
        TextColor = Colors.Black,
        LineBreakMode = LineBreakMode.TailTruncation,
        VerticalOptions = LayoutOptions.Center
    };
    locationLabel.SetBinding(Label.TextProperty, nameof(RadarFlatItem.Location));

    Grid.SetColumn(timeLabel, 0);
    Grid.SetColumn(locationLabel, 1);
    grid.Children.Add(timeLabel);
    grid.Children.Add(locationLabel);

    inner.Content = grid;
    outer.Content = inner;
    return outer;
})
        };

        private readonly RadarParser _parser;
        private List<RadarData> _currentRadars;
        private Canton? _selectedCanton = Canton.Srednjobosanski;
        private readonly LocationTrackingService _locationService;
        private readonly RadarAlertService _alertService;
        private readonly MapDataService _mapDataService;
        private readonly RadarHistoryService _historyService;
        private bool _isMapLoaded = false;
        private bool _isMapJsReady = false;
        private bool _isTrackingActive = false;
        private bool _isMenuOpen = false;
        private bool _isFirstMapLoad = true;
        private bool _isListViewActive = true;
        private bool _isFirstListLoad = true;
        private bool _isDropdownOpen = false;
        private bool _initialCameraPositioned = false;

        private List<CantonPickerItem> _cantonList;
        private CantonPickerItem _currentSelectedItem;

        public MainPage()
        {
            InitializeComponent();
            _parser = new RadarParser();
            _locationService = new LocationTrackingService();
            _alertService = new RadarAlertService();
            _mapDataService = new MapDataService();
            _historyService = new RadarHistoryService();

            MapView.Navigated += OnMapNavigated;
            MapView.Navigating += OnWebViewNavigating;

            SetupServiceEvents();
            _ = LoadRadarDataAsync();
            InitializeCantonPicker();
        }

        private void InitializeCantonPicker()
        {
            _cantonList = new List<CantonPickerItem>
            {
                new CantonPickerItem { Label = "Unsko-sanski kanton",                Value = Canton.UnskoSanski },
                new CantonPickerItem { Label = "Posavski kanton",                    Value = Canton.Posavski },
                new CantonPickerItem { Label = "Tuzlanski kanton",                   Value = Canton.Tuzlanski },
                new CantonPickerItem { Label = "Zeničko-dobojski kanton",            Value = Canton.ZenickoDobojski },
                new CantonPickerItem { Label = "Bosansko-podrinjski kanton", Value = Canton.BosanskoPodrinjski },
                new CantonPickerItem { Label = "Srednjobosanski kanton",             Value = Canton.Srednjobosanski },
                new CantonPickerItem { Label = "Hercegovačko-neretvanski kanton",    Value = Canton.HercegovackoNeretvanski },
                new CantonPickerItem { Label = "Zapadnohercegovački kanton",         Value = Canton.Zapadnohercegovacki },
                new CantonPickerItem { Label = "Kanton Sarajevo",                    Value = Canton.Sarajevo },
                new CantonPickerItem { Label = "Kanton 10",                          Value = Canton.Kanton10 },
            };

            _currentSelectedItem = _cantonList.FirstOrDefault(c => c.Value == _selectedCanton);
            if (_currentSelectedItem != null)
            {
                _currentSelectedItem.IsSelected = true;
                LblSelectedCanton.Text = _currentSelectedItem.Label;
            }

            CantonCollectionView.ItemsSource = _cantonList;
        }

        private void OnOpenPickerClicked(object sender, EventArgs e)
        {
            if (_isDropdownOpen) CloseDropdown();
            else OpenDropdown();
        }
        private void OpenDropdown()
{
    _isDropdownOpen = true;
    DropdownDismissOverlay.IsVisible = true;
    LblPickerArrow.Text = "▲";

    double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
    double rightPadding = 25;
    double dropdownWidth = 220;
    double xPos = screenWidth - dropdownWidth - rightPadding;
    AbsoluteLayout.SetLayoutBounds(PickerDropdown, new Rect(xPos, 58, dropdownWidth, 420));

    PickerDropdown.IsVisible = true;
}

private void CloseDropdown()
{
    _isDropdownOpen = false;
    PickerDropdown.IsVisible = false;
    DropdownDismissOverlay.IsVisible = false;
    LblPickerArrow.Text = "▼";
}
        private void OnDropdownDismissOverlayTapped(object sender, EventArgs e)
        {
            CloseDropdown();
        }

        private async void OnCantonSelected(object sender, SelectionChangedEventArgs e)
        {
            var newItem = e.CurrentSelection.FirstOrDefault() as CantonPickerItem;

            if (sender is CollectionView cv) cv.SelectedItem = null;

            if (newItem == null || newItem == _currentSelectedItem) return;

            if (_currentSelectedItem != null)
            {
                var oldItem = _currentSelectedItem;
                var oldCell = FindCellForItem(CantonCollectionView, oldItem);
                if (oldCell != null)
                {
                    _ = oldCell.FadeTo(0.4, 100).ContinueWith(_ =>
                    {
                        oldItem.IsSelected = false;
                        MainThread.BeginInvokeOnMainThread(async () =>
                            await oldCell.FadeTo(1.0, 150));
                    });
                }
                else
                {
                    oldItem.IsSelected = false;
                }
            }

            var newCell = FindCellForItem(CantonCollectionView, newItem);
            newItem.IsSelected = true;

            if (newCell != null)
            {
                await newCell.ScaleTo(0.93, 80, Easing.CubicIn);
                await newCell.ScaleTo(1.0, 130, Easing.CubicOut);
            }

            _currentSelectedItem = newItem;
            _selectedCanton = newItem.Value;
            LblSelectedCanton.Text = newItem.Label;

            CloseDropdown();
            DisplayRadarData(_currentRadars);
        }
        private VisualElement FindCellForItem(IVisualTreeElement parent, object item)
        {
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is VisualElement ve && ve.BindingContext == item)
                    return ve;

                var found = FindCellForItem(child, item);
                if (found != null) return found;
            }
            return null;
        }

        private async Task ShowCustomAlert(string title, string message, string buttonText = "U REDU")
        {
            CustomAlertTitle.Text = title;
            CustomAlertMessage.Text = message;
            CustomAlertButton.Text = buttonText;
            CustomAlertOverlay.IsVisible = true;

            CustomAlertFrame.Scale = 0.8;
            CustomAlertFrame.Opacity = 0;

            await Task.WhenAll(
                CustomAlertFrame.ScaleTo(1, 250, Easing.CubicOut),
                CustomAlertFrame.FadeTo(1, 250)
            );
        }

        private async void OnCustomAlertButtonClicked(object sender, EventArgs e)
        {
            await Task.WhenAll(
                CustomAlertFrame.ScaleTo(0.8, 200, Easing.CubicIn),
                CustomAlertFrame.FadeTo(0, 200)
            );
            CustomAlertOverlay.IsVisible = false;
        }

        private void SetupServiceEvents()
        {
            _locationService.LocationChanged += async (sender, location) =>
            {
                await OnLocationUpdated(location);
            };

            _locationService.CompassChanged += async     (sender, heading) =>
            {
                if (_locationService.CurrentLocation != null && _isTrackingActive)
                {
                    await UpdateUserLocationOnMap(_locationService.CurrentLocation, heading);
                }
            };

            _alertService.InsideZoneChanged += (sender, isInside) =>
            {
                System.Diagnostics.Debug.WriteLine(isInside ? "Ušli ste u radar zonu!" : "Izašli ste iz radar zone.");
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!await CheckInternetConnectionAsync())
                return;

            if (!_isListViewActive)
            {
                if (_isTrackingActive)
                {
                    await _locationService.StartContinuousTrackingAsync();
                    _locationService.StartCompass();
                }
                else
                {
                    _locationService.StartPeriodicLocationUpdates();
                }

                if (_locationService.CurrentLocation != null && _isMapLoaded)
                {
                    await UpdateUserLocationOnMap(
                        _locationService.CurrentLocation,
                        _isTrackingActive ? _locationService.CurrentHeading : 0);
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _locationService.StopPeriodicLocationUpdates();
            _locationService.StopContinuousTracking();
            _locationService.StopCompass();
            _alertService.StopAlerts();
        }
        private async void OnBurgerMenuClicked(object sender, EventArgs e)
        {
            if (_isDropdownOpen) CloseDropdown();
            if (_isMenuOpen) await CloseMenu();
            else await OpenMenu();
        }

        private async void OnCloseMenuClicked(object sender, EventArgs e)
        {
            await CloseMenu();
        }

        private async Task OpenMenu()
        {
            _isMenuOpen = true;
            BurgerMenuFrame.IsVisible = true;
            Overlay.IsVisible = true;

            var menuAnimation = BurgerMenuFrame.TranslateTo(0, 0, 250, Easing.CubicOut);
            var overlayAnimation = Overlay.FadeTo(0.5, 250);
            await Task.WhenAll(menuAnimation, overlayAnimation);

            Overlay.InputTransparent = false;

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await CloseMenu();
            Overlay.GestureRecognizers.Clear();
            Overlay.GestureRecognizers.Add(tapGesture);
        }

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

        private async void OnListaClicked(object sender, EventArgs e)
        {
            if (!await CheckInternetConnectionAsync()) return;
            await CloseMenu();
            BtnLista.BackgroundColor=Colors.White;
            BtnLista.TextColor = Color.FromArgb("#212143");
            BtnMapa.BackgroundColor = Color.FromArgb("#212143");
            BtnMapa.TextColor=Colors.White;
            BtnHistorija.BackgroundColor = Color.FromArgb("#212143");
            BtnHistorija.TextColor=Colors.White;

            ResetHistoryView();
            HistoryViewContainer.IsVisible = false;
            ListViewContainer.IsVisible = true;
            MapViewContainer.IsVisible = false;
            _isListViewActive = true;

            _locationService.StopPeriodicLocationUpdates();
            _locationService.StopContinuousTracking();
            _locationService.StopCompass();
            _alertService.StopAlerts();
        }

        private async void OnMapaClicked(object sender, EventArgs e)
        {
            if (!await CheckInternetConnectionAsync()) return;
            await CloseMenu();

            BtnMapa.BackgroundColor = Colors.White;
            BtnMapa.TextColor=Color.FromArgb("#212143");
            BtnLista.BackgroundColor = Color.FromArgb("#212143");
            BtnLista.TextColor=Colors.White;
            BtnHistorija.BackgroundColor = Color.FromArgb("#212143");
            BtnHistorija.TextColor=Colors.White;
            ResetHistoryView();
            HistoryViewContainer.IsVisible = false;
            ListViewContainer.IsVisible = false;
            MapViewContainer.IsVisible = true;
            _isListViewActive = false;

            if (_isFirstMapLoad)
            {
                await InitializeMapAsync();
                _isFirstMapLoad = false;
            }
            else
            {
                if (_isTrackingActive)
                {
                    await _locationService.StartContinuousTrackingAsync();
                    _locationService.StartCompass();
                }
                else
                {
                    _locationService.StartPeriodicLocationUpdates();
                }
            }
        }
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadRadarDataAsync();
        }

        private async Task LoadRadarDataAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                BtnRefresh.IsEnabled = false;

                _currentRadars = await _parser.ParseAllLocationsAsync();
                DisplayRadarData(_currentRadars);
                await _historyService.SaveRadarsForDateAsync(DateTime.Now, _currentRadars);
                _isFirstListLoad = false;
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

        private void DisplayRadarData(List<RadarData> radars)
        {
            if (radars == null || radars.Count == 0)
            {
                LblNemaData.IsVisible = true;
                RadarListContainer.ItemsSource = null;
                return;
            }

            LblNemaData.IsVisible = false;

            var filteredRadars = radars.AsEnumerable();
            if (_selectedCanton.HasValue)
            {
                var citiesInCanton = RadarConfig.Locations
                    .Where(l => l.Canton == _selectedCanton.Value)
                    .Select(l => l.Name)
                    .ToHashSet();

                filteredRadars = filteredRadars.Where(r => citiesInCanton.Contains(r.City));
            }

            var flatList = new List<RadarFlatItem>();

            var cityGroups = filteredRadars
                .GroupBy(r => r.City)
                .Select(g => new { CityName = g.Key, Radars = g.ToList() })
                .ToList();

            foreach (var group in cityGroups)
            {
                flatList.Add(new RadarFlatItem
                {
                    Position = RadarGroupPosition.Header,
                    CityName = group.CityName
                });

                var radarList = group.Radars;
                for (int i = 0; i < radarList.Count; i++)
                {
                    var r = radarList[i];
                    bool isFirst = i == 0;
                    bool isLast = i == radarList.Count - 1;

                    RadarGroupPosition pos;
                    if (radarList.Count == 1) pos = RadarGroupPosition.Only;
                    else if (isFirst) pos = RadarGroupPosition.First;
                    else if (isLast) pos = RadarGroupPosition.Last;
                    else pos = RadarGroupPosition.Middle;

                    flatList.Add(new RadarFlatItem
                    {
                        Position = pos,
                        CityName = group.CityName,
                        Time = r.Time,
                        Location = r.Location
                    });
                }
            }

            RadarListContainer.ItemsSource = flatList;
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            var current = Connectivity.NetworkAccess;
            if (current != NetworkAccess.Internet)
            {
                await ShowCustomAlert("Nema internet konekcije", "Molimo provjerite da li su uključeni WiFi ili mobilni podaci.", "U REDU");
                return false;
            }
            return true;
        }
    }


    public class RadarListBindingContext
    {
        public List<CityGroupViewModel> CityGroups { get; set; }
    }
    public class BoolToFontAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is bool b && b ? FontAttributes.Bold : FontAttributes.None;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}
