using RadarApp.Models;
using RadarApp.Services;

namespace RadarApp;

public partial class MainPage
{
    private bool _isHistoryViewActive = false;
    private bool _isCalendarViewShowing = true;
    private DateTime _selectedHistoryDate;
    private DateTime _currentCalendarMonth = DateTime.Today;
    private List<DateTime> _availableHistoryDates = new();

    private List<CantonPickerItem> _historyCantonList;
    private CantonPickerItem _historyCurrentSelectedItem;
    private Canton? _historySelectedCanton = Canton.Srednjobosanski;
    private bool _isHistoryDropdownOpen = false;

    private List<RadarData> _currentHistoryRadars = new();

    private void InitializeHistoryCantonPicker()
    {
        _historyCantonList = new List<CantonPickerItem>
        {
            new CantonPickerItem { Label = "Unsko-sanski kanton",                Value = Canton.UnskoSanski },
            new CantonPickerItem { Label = "Posavski kanton",                    Value = Canton.Posavski },
            new CantonPickerItem { Label = "Tuzlanski kanton",                   Value = Canton.Tuzlanski },
            new CantonPickerItem { Label = "Zeničko-dobojski kanton",            Value = Canton.ZenickoDobojski },
            new CantonPickerItem { Label = "Bosansko-podrinjski kanton",         Value = Canton.BosanskoPodrinjski },
            new CantonPickerItem { Label = "Srednjobosanski kanton",             Value = Canton.Srednjobosanski },
            new CantonPickerItem { Label = "Hercegovačko-neretvanski kanton",    Value = Canton.HercegovackoNeretvanski },
            new CantonPickerItem { Label = "Zapadnohercegovački kanton",         Value = Canton.Zapadnohercegovacki },
            new CantonPickerItem { Label = "Kanton Sarajevo",                    Value = Canton.Sarajevo },
            new CantonPickerItem { Label = "Kanton 10",                          Value = Canton.Kanton10 },
            new CantonPickerItem { Label = "Brčko distrikt",                          Value = Canton.BrckoDistrikt },
        };

        _historyCurrentSelectedItem = _historyCantonList.FirstOrDefault(c => c.Value == _historySelectedCanton);
        if (_historyCurrentSelectedItem != null)
        {
            _historyCurrentSelectedItem.IsSelected = true;
            LblHistorySelectedCanton.Text = _historyCurrentSelectedItem.Label;
        }

        HistoryCantonCollectionView.ItemsSource = _historyCantonList;
    }


    private void OnHistoryOpenPickerClicked(object sender, EventArgs e)
    {
        if (_isHistoryDropdownOpen) CloseHistoryDropdown();
        else OpenHistoryDropdown();
    }

    private void OpenHistoryDropdown()
    {
        _isHistoryDropdownOpen = true;
        HistoryDropdownDismissOverlay.IsVisible = true;
        LblHistoryPickerArrow.Text = "▲";

        double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        double rightPadding = 25;
        double dropdownWidth = 220;
        double xPos = screenWidth - dropdownWidth - rightPadding;
        AbsoluteLayout.SetLayoutBounds(HistoryPickerDropdown, new Rect(xPos, 58, dropdownWidth, 420));

        HistoryPickerDropdown.IsVisible = true;
    }

    private void CloseHistoryDropdown()
    {
        _isHistoryDropdownOpen = false;
        HistoryPickerDropdown.IsVisible = false;
        HistoryDropdownDismissOverlay.IsVisible = false;
        LblHistoryPickerArrow.Text = "▼";
    }

    private void OnHistoryDropdownDismissOverlayTapped(object sender, EventArgs e)
    {
        CloseHistoryDropdown();
    }

    private async void OnHistoryCantonSelected(object sender, SelectionChangedEventArgs e)
    {
        var newItem = e.CurrentSelection.FirstOrDefault() as CantonPickerItem;

        if (sender is CollectionView cv) cv.SelectedItem = null;

        if (newItem == null || newItem == _historyCurrentSelectedItem) return;

        if (_historyCurrentSelectedItem != null)
        {
            var oldItem = _historyCurrentSelectedItem;
            var oldCell = FindCellForItem(HistoryCantonCollectionView, oldItem);
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

        var newCell = FindCellForItem(HistoryCantonCollectionView, newItem);
        newItem.IsSelected = true;

        if (newCell != null)
        {
            await newCell.ScaleTo(0.93, 80, Easing.CubicIn);
            await newCell.ScaleTo(1.0, 130, Easing.CubicOut);
        }

        _historyCurrentSelectedItem = newItem;
        _historySelectedCanton = newItem.Value;
        LblHistorySelectedCanton.Text = newItem.Label;

        CloseHistoryDropdown();

        DisplayHistoryData(_currentHistoryRadars);
    }


    private async Task EnsureTodayDataExistsAsync()
    {
        try
        {
            bool todayExists = await _historyService.CheckIfTodayExistsAsync();

            if (!todayExists)
            {
                var parser = new RadarParser();
                var radars = await parser.ParseAllLocationsAsync();

                var validRadars = radars
                    .Where(r => r.Time != "INFO" && r.Time != "GREŠKA")
                    .ToList();

                if (validRadars.Any())
                    await _historyService.SaveRadarsForDateAsync(DateTime.Today, validRadars);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri osiguravanju današnjih podataka: {ex.Message}");
        }
    }


    public async void OnHistorijaClicked(object sender, EventArgs e)
    {
        if (!await CheckInternetConnectionAsync())
            return;
        await CloseMenu();

        BtnHistorija.BackgroundColor = Colors.White;
        BtnHistorija.TextColor = Color.FromArgb("#212143");
        BtnLista.BackgroundColor = Color.FromArgb("#212143");
        BtnLista.TextColor = Colors.White;
        BtnMapa.BackgroundColor = Color.FromArgb("#212143");
        BtnMapa.TextColor = Colors.White;

        ListViewContainer.IsVisible = false;
        MapViewContainer.IsVisible = false;
        HistoryViewContainer.IsVisible = true;
        _isListViewActive = false;
        _isHistoryViewActive = true;

        _locationService.StopPeriodicLocationUpdates();
        _locationService.StopContinuousTracking();
        _locationService.StopCompass();
        _alertService.StopAlerts();

        await EnsureTodayDataExistsAsync();
        await LoadCalendarDatesAsync();
    }


    private async Task LoadCalendarDatesAsync()
    {
        _availableHistoryDates = await _historyService.GetAvailableDatesAsync();
        GenerateCalendar(_currentCalendarMonth);
    }

    private void OnPrevMonthClicked(object sender, EventArgs e)
    {
        _currentCalendarMonth = _currentCalendarMonth.AddMonths(-1);
        GenerateCalendar(_currentCalendarMonth);
    }

    private void OnNextMonthClicked(object sender, EventArgs e)
    {
        _currentCalendarMonth = _currentCalendarMonth.AddMonths(1);
        GenerateCalendar(_currentCalendarMonth);
    }

    private void GenerateCalendar(DateTime month)
    {
        CalendarGrid.Children.Clear();

        if (CalendarGrid.ColumnDefinitions.Count == 0)
        {
            for (int i = 0; i < 7; i++)
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        var firstDay = new DateTime(month.Year, month.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        int startColumn = ((int)firstDay.DayOfWeek + 6) % 7;

        int totalCells = startColumn + daysInMonth;
        int rowsNeeded = (int)Math.Ceiling(totalCells / 7.0);

        CalendarGrid.RowDefinitions.Clear();
        for (int i = 0; i < rowsNeeded; i++)
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        int currentRow = 0;
        int currentCol = 0;

        for (int i = 0; i < startColumn; i++)
        {
            CalendarGrid.Add(new BoxView { IsVisible = false }, currentCol++, currentRow);
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            if (currentCol == 7)
            {
                currentCol = 0;
                currentRow++;
            }

            var date = new DateTime(month.Year, month.Month, day);
            bool hasData = _availableHistoryDates.Any(d => d.Date == date.Date);
            bool isToday = date.Date == DateTime.Today;

            var frame = new Frame
            {
                Padding = 3,
                CornerRadius = 5,
                BackgroundColor = isToday ? Color.FromArgb("#212143")
                                 : hasData ? Color.FromArgb("#ECF0F1")
                                 : Color.FromArgb("#F8F9F9"),
                HasShadow = false,
                BorderColor = Colors.Transparent
            };

            var label = new Label
            {
                Text = day.ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14,
                TextColor = isToday ? Colors.White
                            : hasData ? Color.FromArgb("#2C3E50")
                            : Colors.Gray
            };

            frame.Content = label;

            var tap = new TapGestureRecognizer();
            var capturedDate = date;

            if (hasData)
            {
                tap.Tapped += async (s, e) =>
                {
                    _selectedHistoryDate = capturedDate;
                    await ShowHistoryForDateAsync(capturedDate);
                };
            }
            else
            {
                tap.Tapped += async (s, e) =>
                {
                    await ShowCustomAlert("Informacija", "Nema snimljenih podataka za odabrani datum.", "U REDU");
                };
            }

            frame.GestureRecognizers.Add(tap);
            CalendarGrid.Add(frame, currentCol++, currentRow);
        }

        LblCalendarMonth.Text = month.ToString("MMMM yyyy");
    }

    private async Task ShowHistoryForDateAsync(DateTime date)
    {
        try
        {
            CalendarFrame.IsVisible = false;
            HistoryDataContainer.IsVisible = true;
            BtnBackToCalendar.IsVisible = true;
            BtnBurgerMenuHistory.IsVisible = false;
            _isCalendarViewShowing = false;

            _selectedHistoryDate = date;
            LblHistoryTitle.Text = date.ToString("dd.MM.yyyy");
            

            if (_historyCantonList == null)
                InitializeHistoryCantonPicker();

            await LoadHistoryDataForDate(date);
        }
        catch (Exception ex)
        {
            await ShowCustomAlert("Greška", $"Greška pri učitavanju podataka: {ex.Message}");
        }
    }

    private async Task LoadHistoryDataForDate(DateTime date)
    {
        HistoryLoadingIndicator.IsRunning = true;
        HistoryLoadingIndicator.IsVisible = true;
        HistoryRadarListContainer.IsVisible = false;
        LblHistoryNemaData.IsVisible = false;

        try
        {
            var radars = await _historyService.LoadRadarsForDateAsync(date);

            _currentHistoryRadars = radars;

            DisplayHistoryData(radars);
        }
        finally
        {
            HistoryLoadingIndicator.IsRunning = false;
            HistoryLoadingIndicator.IsVisible = false;
        }
    }

    private void DisplayHistoryData(List<RadarData> radars)
    {
        if (radars == null || !radars.Any())
        {
            LblHistoryNemaData.IsVisible = true;
            HistoryRadarListContainer.IsVisible = false;
            HistoryRadarListContainer.ItemsSource = null;
            return;
        }

        var filteredRadars = radars.AsEnumerable();
        if (_historySelectedCanton.HasValue)
        {
            var citiesInCanton = RadarConfig.Locations
                .Where(l => l.Canton == _historySelectedCanton.Value)
                .Select(l => l.Name)
                .ToHashSet();

            filteredRadars = filteredRadars.Where(r => citiesInCanton.Contains(r.City));
        }

        var validRadars = filteredRadars
            .Where(r => r.Time != "INFO" && r.Time != "GREŠKA")
            .ToList();

        if (!validRadars.Any())
        {
            LblHistoryNemaData.IsVisible = true;
            HistoryRadarListContainer.IsVisible = false;
            HistoryRadarListContainer.ItemsSource = null;
            return;
        }

        LblHistoryNemaData.IsVisible = false;

        var flatList = new List<RadarFlatItem>();

        var cityGroups = validRadars
            .GroupBy(r => r.City)
            .OrderByDescending(g=>g.Key)
            .Select(g => new { CityName = g.Key, Radars = g.OrderBy(r => r.Time).ToList() })
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

        HistoryRadarListContainer.ItemsSource = flatList;
        HistoryRadarListContainer.IsVisible = true;
    }


    private void OnBackToCalendarClicked(object sender, EventArgs e)
    {
        if (_isHistoryDropdownOpen) CloseHistoryDropdown();

        _historySelectedCanton = Canton.Srednjobosanski;

        if (_historyCantonList != null)
        {
            foreach (var item in _historyCantonList)
            {
                item.IsSelected = (item.Value == Canton.Srednjobosanski);
                if (item.IsSelected)
                {
                    _historyCurrentSelectedItem = item;
                    LblHistorySelectedCanton.Text = item.Label;
                }
            }
            
            HistoryCantonCollectionView.ItemsSource = null;
            HistoryCantonCollectionView.ItemsSource = _historyCantonList;
        }

        CalendarFrame.IsVisible = true;
        HistoryDataContainer.IsVisible = false;
        BtnBackToCalendar.IsVisible = false;
        BtnBurgerMenuHistory.IsVisible = true;
        _isCalendarViewShowing = true;
        LblHistoryTitle.Text = "Historija Radara";
        
        _currentHistoryRadars = new List<RadarData>();
    }
    private void ResetHistoryView()
    {
        if (_isHistoryDropdownOpen) CloseHistoryDropdown();

        CalendarFrame.IsVisible = true;
        HistoryDataContainer.IsVisible = false;
        BtnBackToCalendar.IsVisible = false;
        BtnBurgerMenuHistory.IsVisible = true;
        _isCalendarViewShowing = true;
        _currentHistoryRadars = new();

        if (HistoryRadarListContainer != null)
            HistoryRadarListContainer.ItemsSource = null;

        LblHistoryTitle.Text = "Historija Radara";
        _currentCalendarMonth = DateTime.Today;
        
        _historySelectedCanton = Canton.Srednjobosanski;
        _historyCantonList = null; 
    }
}