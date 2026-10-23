using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlotThoseLines.MyClass;

public class Filter : ObservableObject
{
    private DateTimeOffset? _startDate;
    private DateTimeOffset? _endDate;

    public DateTimeOffset? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTimeOffset? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public Filter(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public void toString()
    {
        Console.WriteLine($"Start Date: {StartDate}, End Date: {EndDate}");
    }
}
