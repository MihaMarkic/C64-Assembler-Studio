using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Desktop.Views.Tools;

public partial class Breakpoints : UserControl
{
    public Breakpoints()
    {
        InitializeComponent();
        Grid.LoadingRow += GridOnLoadingRow;
    }
    
    private void GridOnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        var row = e.Row;
        row.BindClass("error", new Binding
        {
            Path = nameof(BreakpointViewModel.HasErrors),
            Source = row.DataContext,
        }, null!);
    }
}