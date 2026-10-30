using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlotThoseLines.MyClass;

public class Filter : ObservableObject
{
    private string? _startDate;
    private string? _endDate;

    public string? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public string? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public Filter(string? startDate, string? endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public override string ToString()
    {
        return $"Start Date: {StartDate}, End Date: {EndDate}";
    }

}

public static class FilterExtensions
{
    public static bool IsABiggerDateThan(this int date, int otherDate)
    {
        return date > otherDate;
    }
}
