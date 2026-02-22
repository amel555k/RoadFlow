using System.ComponentModel;
using System.Runtime.CompilerServices;
using RadarApp.Models;

public class CantonPickerItem : INotifyPropertyChanged
    {
        public string Label { get; set; }
        public Canton? Value { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BackgroundColor));
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        public Color BackgroundColor => IsSelected ? Colors.White : Color.FromArgb("#4F6F91");
        public Color TextColor => IsSelected ? Color.FromArgb("#4F6F91") : Colors.White;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }