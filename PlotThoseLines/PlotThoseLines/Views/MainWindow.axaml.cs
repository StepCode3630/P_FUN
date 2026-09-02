using Avalonia.Controls;
using ScottPlot.Avalonia;

namespace PlotThoseLines.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        double[] dataX = { 1, 2, 3, 4, 5 };
        double[] dataY = { 1, 4, 9, 16, 25 };

        AvaPlot avaPlot1 = this.Find<AvaPlot>("AvaPlot1");

        avaPlot1.Plot.Add.Scatter(dataX, dataY);
        avaPlot1.Refresh();

    }

}