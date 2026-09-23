using CommunityToolkit.Mvvm.ComponentModel;
using PlotThoseLines.MyClass;
using System.Collections.ObjectModel;

namespace PlotThoseLines.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public ObservableCollection<ListeMenuItem> Listes { get; } =
       new()
       {
            new ListeMenuItem("Liste 1"),
            new ListeMenuItem("Liste 2"),
            new ListeMenuItem("Liste 3"),
       };
}
