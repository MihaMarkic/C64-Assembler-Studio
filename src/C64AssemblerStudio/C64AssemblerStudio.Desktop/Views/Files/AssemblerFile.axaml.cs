using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Desktop.Views.Files;

public partial class AssemblerFile : UserControl
{
    public AssemblerFile()
    {
        InitializeComponent();
    }
    internal FileViewModel? ViewModel => (FileViewModel?)base.DataContext;
    private void Editor_PointerHover(object? sender, PointerEventArgs e)
    {
        // if (ViewModel is not null)
        // {
        //     var flyout = (Flyout?)FlyoutBase.GetAttachedFlyout(Editor);
        //     if (flyout is not null)
        //     {
        //         object? symbolReference = GetSymbolReferenceAtPosition(e);
        //         if (symbolReference is not null)
        //         {
        //             var info = ViewModel.GetContextSymbolReferenceInfo(symbolReference);
        //             if (info is not null)
        //             {
        //                 FlyoutContent.DataContext = info;
        //                 flyout.ShowAt(Editor, true);
        //             }
        //         }
        //     }
        // }
    }

    private void Editor_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // if (ViewModel is not null)
        // {
        //     var point = e.GetCurrentPoint(sender as Control);
        //     if (point.Properties.IsRightButtonPressed)
        //     {
        //         ViewModel.ContextSymbolReference = GetSymbolReferenceAtPosition(e);
        //     }
        // }
    }
}