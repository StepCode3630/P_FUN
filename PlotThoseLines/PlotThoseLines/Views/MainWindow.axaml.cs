using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using CsvHelper.Configuration;
using PlotThoseLines.MyClass;
using PlotThoseLines.ViewModels;
using PlotThoseLines.Views;
using ScottPlot;
using ScottPlot.ArrowShapes;
using ScottPlot.Avalonia;
using ScottPlot.Colormaps;
using ScottPlot.Plottables;
using Tmds.DBus.Protocol;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace PlotThoseLines.Views;

public partial class MainWindow : Window
{
    public ObservableCollection<Dictionary<string, string>> Rows { get; } = new();
    public ObservableCollection<string> AvailableColumns { get; } = new();
    public ObservableCollection<string> FiltersDateList { get; } = new();

    public ObservableCollection<ColumnToggle> ColumnToggles { get; } =
        new() { new ColumnToggle("Courbe test 1"), new ColumnToggle("Courbe test 2") };

    public string? SelectedXColumn { get; set; }
    public string? SelectedYColumn { get; set; }
    private List<double> _xs = new();
    private List<double> _ys = new();

    public void ApplyFilter_Click(object? sender, RoutedEventArgs e)
    {
        FiltersDateList.Clear();

        var _viewModel = (MainViewModel)DataContext;

        if (!_viewModel.StartDate.HasValue || !_viewModel.EndDate.HasValue)
        {
            Console.WriteLine("Select a start date and an end date.");
            return;
        }

        if (_viewModel.StartDate > _viewModel.EndDate)
        {
            Console.WriteLine("Start date must be earlier than end date.");
            return;
        }

        var startDate = _viewModel.StartDate.Value.Year;
        var endDate = _viewModel.EndDate.Value.Year;

        // for (int i = startDate; i < endDate; i++)
        // {
        //     FiltersDateList.Add(Convert.ToString(i));
        // }

        Enumerable
            .Range(startDate, endDate - startDate)
            .Select(year => year.ToString())
            .ToList()
            .ForEach(year => FiltersDateList.Add(year));

        Console.WriteLine($"Filter applied: {string.Join(", ", FiltersDateList)}");

        InitializePlot("jsp", false);
    }

    private async Task<PlotSelectResult?> ShowOptions()
    {
        var columnNames = AvailableColumns.ToList();
        var dialog = new PlotOptionsWindow(columnNames);
        PlotSelectResult? result = await dialog.ShowDialog<PlotSelectResult?>(this);

        return result;
    }

    private void ImportCSV(string filePath)
    {
        FiltersDateList.Clear();

        var viewModel = (MainViewModel)DataContext;
        viewModel.HasYearRange = false;
        viewModel.StartDate = null;
        viewModel.EndDate = null;
        Rows.Clear();
        AvailableColumns.Clear();
        SelectedXColumn = null;
        SelectedYColumn = null;

        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var config = new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader())
            return;

        string[]? headers = csv.HeaderRecord;
        if (headers is null || headers.Length == 0)
            return;

        // foreach (string header in headers)
        // {
        //     if (string.IsNullOrWhiteSpace(header))
        //         continue;

        //     AvailableColumns.Add(header);
        // }

        // filtrer les colonnes vides et les ajouter à AvailableColumns
        headers
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .ToList()
            .ForEach(header => AvailableColumns.Add(header));

        while (csv.Read())
        {
            //var row = new Dictionary<string, string>();
            // foreach (string header in headers)
            //     row[header] = csv.GetField(header) ?? string.Empty;

            var row = headers.ToDictionary(
                header => header,
                header => csv.GetField(header) ?? string.Empty
            );

            Rows.Add(row);
        }

        if (AvailableColumns.Count >= 2)
        {
            SelectedXColumn = AvailableColumns[0];
            SelectedYColumn = AvailableColumns[1];
        }
    }

    private void PreparePlotData(string xColumn, string yColumn)
    {
        _xs.Clear();
        _ys.Clear();

        if (yColumn == null & xColumn == null)
            throw new Exception();

        var points = Rows.Select(row =>
            {
                bool xIsValid = double.TryParse(
                    row[xColumn],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double x
                );

                bool yIsValid = double.TryParse(
                    row[yColumn],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double y
                );

                return (xIsValid, yIsValid, x, y);
            })
            .Where(p =>
                p.xIsValid
                && p.yIsValid
                && (FiltersDateList.Count == 0 || FiltersDateList.Contains(Convert.ToString(p.x)))
            )
            .ToList();

        _xs.AddRange(points.Select(p => p.x));
        _ys.AddRange(points.Select(p => p.y));
    }

    private async void OnAddList(object? sender, RoutedEventArgs args)
    {
        var customOptions = new FilePickerOpenOptions
        {
            Title = "Choisir un fichier CSV",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Fichiers CSV") { Patterns = new[] { "*.csv" } },
            },
        };

        IReadOnlyList<IStorageFile> storage = await StorageProvider.OpenFilePickerAsync(
            customOptions
        );

        if (storage.Count == 0)
            return;

        IStorageFile file = storage[0];

        string pathFile = file.Path.LocalPath;

        ImportCSV(pathFile);

        if (AvailableColumns.Count < 2)
            return;

        PlotSelectResult? options = await ShowOptions();

        if (options is null)
            return;

        SelectedXColumn = options.XColumn;
        SelectedYColumn = options.YColumn;

        InitializePlot(options.PlotName, true);
    }

    private void InitializePlot(string plotName, bool initializeDateRange)
    {
        AvaPlot? avaPlot1 = this.FindControl<AvaPlot>("AvaPlot1");

        avaPlot1.Plot.Clear();

        var _viewModel = (MainViewModel)DataContext;

        // Use the X values actually plotted, rather than the padded axis limits.
        var plottedYears = new List<double>();
        _viewModel.ColumnToggles.Clear();
        //foreach (string ycolumn in AvailableColumns)
        //{
        //    if (ycolumn == SelectedXColumn)
        //        continue;

        //    PreparePlotData(SelectedXColumn, ycolumn);
        //    plottedYears.AddRange(_xs);

        //    var scatter = avaPlot1.Plot.Add.Scatter(_xs.ToArray(), _ys.ToArray());
        //    scatter.LegendText = ycolumn;

        //    _viewModel.ColumnToggles.Add(
        //        new ColumnToggle(ycolumn)
        //        {
        //            VisibilityChanged = visible =>
        //            {
        //                scatter.IsVisible = visible;
        //                avaPlot1.Refresh();
        //            },
        //        }
        //    );
        //}
        AvailableColumns
            .Where(yColumn => yColumn != SelectedXColumn)
            .ToList()
            .ForEach(yColumn =>
            {
                PreparePlotData(SelectedXColumn, yColumn);
                plottedYears.AddRange(_xs);

                var scatter = avaPlot1.Plot.Add.Scatter(_xs.ToArray(), _ys.ToArray());
                scatter.LegendText = yColumn;

                _viewModel.ColumnToggles.Add(
                    new ColumnToggle(yColumn)
                    {
                        VisibilityChanged = visible =>
                        {
                            scatter.IsVisible = visible;
                            avaPlot1.Refresh();
                        },
                    }
                );
            });

        if (initializeDateRange)
            UpdateYearRange(plottedYears);

        avaPlot1.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        avaPlot1.Plot.Title(plotName);
        avaPlot1.Plot.XLabel(SelectedXColumn);
        avaPlot1.Plot.YLabel(SelectedYColumn);

        avaPlot1.Plot.Axes.AutoScale();
        avaPlot1.Refresh();
    }

    private void UpdateYearRange(List<double> years)
    {
        var viewModel = (MainViewModel)DataContext;
        viewModel.HasYearRange = false;
        viewModel.StartDate = null;
        viewModel.EndDate = null;

        // A DatePicker can only represent whole years from 1 to 9999.
        if (
            years.Count == 0
            || years.Any(year =>
                !double.IsFinite(year) || year < 1 || year > 9999 || year != Math.Truncate(year)
            )
        )
            return;

        var minimum = new DateTimeOffset((int)years.Min(), 1, 1, 0, 0, 0, TimeSpan.Zero);
        var maximum = new DateTimeOffset((int)years.Max(), 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Expand the old bounds first so a new range never temporarily crosses them.
        if (minimum > viewModel.MaximumYear)
            viewModel.MaximumYear = maximum;
        if (maximum < viewModel.MinimumYear)
            viewModel.MinimumYear = minimum;

        viewModel.MinimumYear = minimum;
        viewModel.MaximumYear = maximum;
        viewModel.StartDate = minimum;
        viewModel.EndDate = maximum;
        viewModel.HasYearRange = true;
    }

    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();

        //ImportCSV(
        //    //"/home/patricnystepan/Documents/Github/P_FUN/doc/Fichier import/production_electricite_complete_normalized.csv" ||
        //    "C:\\Users\\pl77sbr\\source\\repos\\P_FUN\\doc\\Fichier import\\production_electricite_complete_normalized.csv"
        //);

        //AvaPlot avaPlot1 = this.Find<AvaPlot>("AvaPlot1");

        //foreach (string yColumn in AvailableColumns)
        //{
        //    //On aime pas les années
        //    if (yColumn == SelectedXColumn)
        //        continue;

        //    PreparePlotData(SelectedXColumn, yColumn);

        //    var scatter = avaPlot1.Plot.Add.Scatter(_xs.ToArray(), _ys.ToArray());
        //    scatter.LegendText = yColumn;
        //}

        //avaPlot1.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        //avaPlot1.Refresh();
    }
}
