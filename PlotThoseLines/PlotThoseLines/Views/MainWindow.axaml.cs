using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CsvHelper;
using CsvHelper.Configuration;
using PlotThoseLines.MyClass;
using PlotThoseLines.ViewModels;
using ScottPlot;
using ScottPlot.ArrowShapes;
using ScottPlot.Avalonia;
using ScottPlot.Colormaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Markup;
using System.Xml.Linq;
using Tmds.DBus.Protocol;

namespace PlotThoseLines.Views;

public partial class MainWindow : Window
{
    public ObservableCollection<Dictionary<string, string>> Rows { get; } = new();
    public ObservableCollection<string> AvailableColumns { get; } = new();
    public string? SelectedXColumn { get; set; }
    public string? SelectedYColumn { get; set; }
    private List<double> _xs = new();
    private List<double> _ys = new();

    private void ImportCSV(string filePath)
    {
        Rows.Clear();
        AvailableColumns.Clear();

        var config = new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        string[]? headers = csv.HeaderRecord;

        foreach (string header in headers)
            AvailableColumns.Add(header);

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
    { // TODO :
        // 0. Vider _xs & _ys
        // 1. Vérifier que SelectedXColumn et SelectedYColumn ne sont pas null
        // 2. Parcourir Rows
        // 3. Récupérer les valeurs avec row[SelectedXColumn]
        // 4. Les convertir en double
        // 5. Ajouter uniquement les lignes valides dans _xs et _ys}

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

    public async void OnAddList(object sender, RoutedEventArgs args)
    {
        var customOptions = new FilePickerOpenOptions
        {
            Title = "Choisir un fichier csv",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Fichiers CSV") { Patterns = new[] { "*.csv" } },
            },
        };
        var storage = await StorageProvider.OpenFilePickerAsync(customOptions);

        var file = storage[0];

        string pathFile = file.Path.LocalPath;

        ImportCSV(pathFile);

        InitializePlot();
    }

    private void InitializePlot()
    {
        AvaPlot? avaPlot1 = this.FindControl<AvaPlot>("AvaPlot1");

        foreach (string yColumn in AvailableColumns)
        {
            //On aime pas les années
            if (yColumn == SelectedXColumn)
                continue;

            PreparePlotData(SelectedXColumn, yColumn);

            var scatter = avaPlot1.Plot.Add.Scatter(_xs.ToArray(), _ys.ToArray());
            scatter.LegendText = yColumn;
        }

        avaPlot1.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        avaPlot1.Refresh();
        avaPlot1.Plot.Axes.AutoScale();
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
