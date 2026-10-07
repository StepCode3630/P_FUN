using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlotThoseLines.ViewModels
{
    public class ColumnToggle : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        public string Name { get; } = "";

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                    return;

                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));

                VisibilityChanged?.Invoke(value);
            }
        }

        public Action<bool>? VisibilityChanged { get; set; }

        public ColumnToggle(string name)
        {
            Name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
