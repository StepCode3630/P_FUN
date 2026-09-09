using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace PlotThoseLines.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        

        AvaPlot avaPlot1 = this.Find<AvaPlot>("AvaPlot1");


        var sig1 = avaPlot1.Plot.Add.Signal(Generate.Sin(51, phase: .2));
        var sig2 = avaPlot1.Plot.Add.Signal(Generate.Sin(51, phase: .4));
        var sig3 = avaPlot1.Plot.Add.Signal(Generate.Sin(51, phase: .6));

        sig1.LegendText = "Signal 1";
        sig2.LegendText = "Signal 2";
        sig3.LegendText = "Signal 3";

        avaPlot1.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);


        avaPlot1.Refresh();
    }
}
