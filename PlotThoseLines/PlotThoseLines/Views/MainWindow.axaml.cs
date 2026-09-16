using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Markup;
using System.Xml.Linq;
using Avalonia.Controls;
using CsvHelper;
using CsvHelper.Configuration;
using ScottPlot;
using ScottPlot.ArrowShapes;
using ScottPlot.Avalonia;
using ScottPlot.Colormaps;
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

    private void PreparePlotData()
    { // TODO :
        // 1. Vérifier que SelectedXColumn et SelectedYColumn ne sont pas null
        // 2. Parcourir Rows
        // 3. Récupérer les valeurs avec row[SelectedXColumn]
        // 4. Les convertir en double
        // 5. Ajouter uniquement les lignes valides dans _xs et _ys}
        if (SelectedYColumn == null & SelectedXColumn == null)
            throw new Exception();
        foreach (var row in Rows)
        {
            string xText = row[SelectedXColumn];
            string yText = row[SelectedYColumn];

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
            _xs.Add(x);
            _ys.Add(y);
        }
    }
    


    public MainWindow()
    {
        InitializeComponent();


        AvaPlot avaPlot1 = this.Find<AvaPlot>("AvaPlot1");



        var sig1 = avaPlot1.Plot.Add.Scatter(_xs, _ys);

        avaPlot1.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);

        avaPlot1.Refresh();
    }
}
