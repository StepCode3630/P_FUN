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
    public string? SelectedXColumn { get; set; }
    public string? SelectedYColumn { get; set; }
    private List<double> _xs = new();
    private List<double> _ys = new();

    private async Task<PlotSelectResult?> ShowOptions()
    {
        var columnNames = AvailableColumns.ToList();
        var dialog = new PlotOptionsWindow(columnNames);
        PlotSelectResult? result = await dialog.ShowDialog<PlotSelectResult?>(this);

        return result;
    }

    private void ImportCSV(string filePath)
    {
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

        foreach (string header in headers)
        {
            if (string.IsNullOrWhiteSpace(header))
                continue;

            AvailableColumns.Add(header);
        }

        while (csv.Read())
        {
            var row = new Dictionary<string, string>();
            foreach (string header in headers)
                row[header] = csv.GetField(header) ?? string.Empty;
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

        foreach (var row in Rows)
        {
            string xText = row[xColumn];
            string yText = row[yColumn];

            bool xIsValid = double.TryParse(
                xText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double x
            );
            bool yIsValid = double.TryParse(
                yText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double y
            );

            if (!xIsValid || !yIsValid)
                continue;

            _xs.Add(x);
            _ys.Add(y);
        }
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
        SelectedYColumn = null;


        InitializePlot(options.PlotName);
    }

    private void InitializePlot(string plotName)
    {

        AvaPlot? avaPlot1 = this.FindControl<AvaPlot>("AvaPlot1");
            avaPlot1.Plot.Clear();




        foreach (string ycolumn in AvailableColumns)
        {

            if (ycolumn == SelectedXColumn)
                continue;

            PreparePlotData(SelectedXColumn, ycolumn);


            var scatter = avaPlot1.Plot.Add.Scatter(_xs.ToArray(), _ys.ToArray());
            scatter.LegendText = ycolumn;
        }

        avaPlot1.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);

        avaPlot1.Plot.Axes.AutoScale();
        avaPlot1.Refresh();
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
