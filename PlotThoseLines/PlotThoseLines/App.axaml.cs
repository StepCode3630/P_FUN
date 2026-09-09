using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PlotThoseLines.ViewModels;
using PlotThoseLines.Views;

namespace PlotThoseLines;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };

            desktop.MainWindow = mainWindow;

            mainWindow.Opened += async (_, _) => {
                var custom = new CustomPresenter();
                await custom.ShowDialog(mainWindow);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void MainWindow_Opened(object? sender, System.EventArgs e)
    {
        throw new System.NotImplementedException();
    }
}