using Avalonia.Controls;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Views;

public class UserControl<T> : UserControl
    where T: ViewModel
{
    protected T? ViewModel;
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ViewModel = (T?)DataContext;
    }
}