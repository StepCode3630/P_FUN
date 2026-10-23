using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlotThoseLines.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    public ObservableCollection<ColumnToggle> ColumnToggles { get; } = new();

    [ObservableProperty]
    private DateTimeOffset? startDate;

    [ObservableProperty]
    private DateTimeOffset? endDate;
    [ObservableProperty]
    private DateTimeOffset minimumYear = new(1, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [ObservableProperty]
    private DateTimeOffset maximumYear = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [ObservableProperty]
    private bool hasYearRange;
}
