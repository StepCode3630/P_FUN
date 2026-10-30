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
    private decimal startDate;

    [ObservableProperty]
    private decimal endDate;

    [ObservableProperty]
    private decimal minimumYear;

    [ObservableProperty]
    private decimal maximumYear;

    [ObservableProperty]
    private bool hasYearRange;
}
