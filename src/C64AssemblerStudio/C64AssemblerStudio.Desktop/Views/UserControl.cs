using Avalonia.Controls;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Views;

public class UserControl<T> : UserControl
    where T: ViewModel
{
    protected T? ViewModel => (T?)DataContext;
}