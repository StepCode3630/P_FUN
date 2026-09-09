using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PlotThoseLines.Views
{
    public partial class CustomPresenter : Window
    {
        public CustomPresenter()
        {
            InitializeComponent();
        }

        public void CloseWindow(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
