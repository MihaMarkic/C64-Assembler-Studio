using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Desktop.Views.Files;

public partial class AssemblerFile : UserControl
{
    readonly LineNumbersMargin _lineNumbersMargin;
    ISolidColorBrush? _lineNumberForeground;
    public AssemblerFile()
    {
        InitializeComponent();
        var leftMargins = Editor.TextArea.LeftMargins;
        _lineNumbersMargin = new LineNumbersMargin(Editor.FontFamily, Editor.FontSize)
        {
            Foreground = _lineNumberForeground,
            Margin = new Thickness(4, 0),
        };
        leftMargins.Add(_lineNumbersMargin);
        Editor.TextChanged += EditorOnTextChanged;
    }

    private void EditorOnTextChanged(object? sender, EventArgs e)
    {
        _lineNumbersMargin.InvalidateMeasure();    
    }

    internal FileViewModel? ViewModel => (FileViewModel?)base.DataContext;
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (this.TryFindResource("LineNumber", out object? res) && res is ISolidColorBrush brush)
        {
            _lineNumberForeground = brush;
            if (_lineNumbersMargin is not null)
            {
                _lineNumbersMargin.Foreground = _lineNumberForeground;
            }
        }
    }
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