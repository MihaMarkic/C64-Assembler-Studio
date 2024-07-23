using Avalonia.Controls;
using Avalonia.Input;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Desktop.Views.Tools;

public partial class CompilerErrors : UserControl
{
    public CompilerErrors()
    {
        InitializeComponent();
        Grid.DoubleTapped += GridOnDoubleTapped;
    }
    private CompilerErrorsOutputViewModel? ViewModel => (CompilerErrorsOutputViewModel?)DataContext!;
    private void GridOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel is not null)
        {
            var command = ViewModel.JumpToCommand;
            var parameter = (CompilerError)Grid.SelectedItem;
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }
}