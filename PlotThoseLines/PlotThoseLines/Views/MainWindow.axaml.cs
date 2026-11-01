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

    /// <summary>
    /// Applique les filtres de date sélectionnés par l'utilisateur et met à jour la liste FiltersDateList en conséquence. Si les dates sont invalides, un message d'erreur est affiché dans la console. Ensuite, la méthode InitializePlot est appelée pour mettre à jour le graphique avec les nouvelles données filtrées.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ApplyFilter_Click(object? sender, RoutedEventArgs e)
    {
        FiltersDateList.Clear();

        var _viewModel = (MainViewModel)DataContext;

        var startDate = Convert.ToInt32(_viewModel?.StartDate);
        var endDate = Convert.ToInt32(_viewModel?.EndDate);

        if (endDate != startDate && !endDate.IsABiggerDateThan(startDate))
        {
            Console.WriteLine("L'année de fin doit être supérieure ou égale à celle du début");
            return;
        }

        if (!endDate.IsAValidYear() || !startDate.IsAValidYear())
        {
            Console.WriteLine("L'année doit être valide");
            return;
        }

        Enumerable
            .Range(startDate, endDate - startDate + 1)
            .Select(year => year.ToString())
            .ToList()
            .ForEach(year => FiltersDateList.Add(year));

        Console.WriteLine($"Filter applied: {string.Join(", ", FiltersDateList)}");

        InitializePlot("jsp", false);
    }

    /// <summary>
    /// Affiche la fenêtre d'options de tracé et retourne les résultats sélectionnés par l'utilisateur.
    /// </summary>
    /// <returns></returns>
    private async Task<PlotSelectResult?> ShowOptions()
    {
        var columnNames = AvailableColumns.ToList();
        var dialog = new PlotOptionsWindow(columnNames);
        PlotSelectResult? result = await dialog.ShowDialog<PlotSelectResult?>(this);

        return result;
    }

    /// <summary>
    /// Importer un fichier CSV, lire les en-têtes et les lignes, et remplir les collections Rows et AvailableColumns. Si le fichier est vide ou invalide, la méthode retourne sans effectuer d'opérations supplémentaires. Les colonnes vides sont filtrées et les deux premières colonnes disponibles sont sélectionnées par défaut pour l'axe X et l'axe Y.
    /// </summary>
    /// <param name="filePath"></param>
    public void ImportCSV(string filePath)
    {
        FiltersDateList.Clear();

        var viewModel = (MainViewModel)DataContext;
        viewModel.HasYearRange = false;
        viewModel.StartDate = 0;
        viewModel.EndDate = 0;
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

        // filtrer les colonnes vides et les ajouter à AvailableColumns
        headers
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .ToList()
            .ForEach(header => AvailableColumns.Add(header));

        while (csv.Read())
        {
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

    /// <summary>
    /// Prépare les données de tracé en fonction des colonnes X et Y sélectionnées. Les valeurs valides sont extraites des lignes importées et stockées dans les listes _xs et _ys. Si les colonnes X ou Y sont nulles, une exception est levée. Les points invalides ou filtrés par la liste FiltersDateList sont ignorés.
    /// </summary>
    /// <param name="xColumn"></param>
    /// <param name="yColumn"></param>
    public void PreparePlotData(string xColumn, string yColumn)
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

    /// <summary>
    /// Ajoute une nouvelle liste de données à partir d'un fichier CSV sélectionné par l'utilisateur. La méthode ouvre un sélecteur de fichiers, lit le fichier CSV choisi, importe les données et met à jour le graphique avec les colonnes X et Y sélectionnées. Si aucune colonne n'est sélectionnée ou si le fichier est invalide, la méthode retourne sans effectuer d'opérations supplémentaires.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Initialise le graphique avec les données préparées et les colonnes sélectionnées. La méthode efface le graphique existant, ajoute de nouvelles séries de données pour chaque colonne Y disponible, configure les étiquettes et la légende, et ajuste l'échelle des axes. Si initializeDateRange est vrai, la plage d'années est mise à jour en fonction des valeurs tracées.
    /// </summary>
    /// <param name="plotName"></param>
    /// <param name="initializeDateRange"></param>
    private void InitializePlot(string plotName, bool initializeDateRange)
    {
        AvaPlot? avaPlot1 = this.FindControl<AvaPlot>("AvaPlot1");

        avaPlot1.Plot.Clear();

        var _viewModel = (MainViewModel)DataContext;

        // Use the X values actually plotted, rather than the padded axis limits.
        var plottedYears = new List<double>();
        _viewModel.ColumnToggles.Clear();

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

    /// <summary>
    /// Met à jour la plage d'années en fonction des valeurs tracées.
    /// </summary>
    /// <param name="years"></param>
    private void UpdateYearRange(List<double> years)
    {
        var viewModel = (MainViewModel)DataContext;
        viewModel.HasYearRange = false;
        viewModel.StartDate = 0;
        viewModel.EndDate = 0;

        // Only enable the year filter for whole years from 1 to 9999.
        if (
            years.Count == 0
            || years.Any(year =>
                !double.IsFinite(year) || year < 1 || year > 9999 || year != Math.Truncate(year)
            )
        )
            return;

        var minimum = (decimal)years.Min();
        var maximum = (decimal)years.Max();

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
    }
}
