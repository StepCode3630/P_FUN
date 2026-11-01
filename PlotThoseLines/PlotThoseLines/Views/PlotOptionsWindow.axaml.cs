using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlotThoseLines.MyClass;

namespace PlotThoseLines.Views
{
    public partial class PlotOptionsWindow : Window
    {
        private readonly List<string> _columns = new();

        public PlotOptionsWindow()
        {
            InitializeComponent();
        }

        public PlotOptionsWindow(IEnumerable<string> columnNames)
            : this()
        {
            _columns = columnNames
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Select(column => column.Trim())
                .Distinct()
                .ToList();

            XColumnComboBox.ItemsSource = _columns;

            if (_columns.Count > 0)
            {
                XColumnComboBox.SelectedIndex = 0;
            }
        }

        public void ValidateOptions_Click(object? sender, RoutedEventArgs e)
        {
            string plotName = PlotNameTextBox.Text?.Trim() ?? string.Empty;

            string? xColumn = XColumnComboBox.SelectedItem as string ?? string.Empty;
            string? yColumn = YColumnTextBox.Text?.Trim() ?? string.Empty;

            var result = new PlotSelectResult
            {
                PlotName = plotName,
                XColumn = xColumn,
                YColumn = yColumn,
            };

            Close(result);
        }

        private void CancelOptions_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
