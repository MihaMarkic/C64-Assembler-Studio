using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Desktop.Views.Files;

public partial class AssemblerFile : UserControl
{
    readonly LineNumbers _lineNumbers;
    private SyntaxColorizer? _syntaxColorizer;
    ISolidColorBrush? _lineNumberForeground;
    private AssemblerFileViewModel? _oldViewModel;
    public AssemblerFile()
    {
        InitializeComponent();
        var leftMargins = Editor.TextArea.LeftMargins;
        _lineNumbers = new LineNumbers(Editor.FontFamily, Editor.FontSize)
        {
            Foreground = _lineNumberForeground,
            Margin = new Thickness(4, 0),
        };
        leftMargins.Add(_lineNumbers);
        Editor.TextChanged += EditorOnTextChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        DetachOldViewModel();
        if (DataContext is AssemblerFileViewModel fileViewModel)
        {
            _syntaxColorizer = new SyntaxColorizer(fileViewModel);
            Editor.TextArea.TextView.LineTransformers.Add(_syntaxColorizer);
            fileViewModel.PropertyChanged += FileViewModelOnPropertyChanged;
            Editor.Text = fileViewModel.Content;
            _oldViewModel = fileViewModel;
        }
        else
        {
            if (_syntaxColorizer is not null)
            {
                Editor.TextArea.TextView.LineTransformers.Remove(_syntaxColorizer);
            }
            _syntaxColorizer = null;
            _oldViewModel = null;
        }
        base.OnDataContextChanged(e);
    }

    void DetachOldViewModel()
    {
        if (_oldViewModel is not null)
        {
            _oldViewModel.PropertyChanged -= FileViewModelOnPropertyChanged;
        }
    }

    private bool _ignoreTextChange;
    private void FileViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AssemblerFileViewModel.Content):
                if (!_ignoreTextChange)
                {
                    Editor.Text = ViewModel.ValueOrThrow().Content;
                }

                break;
            case nameof(AssemblerFileViewModel.Lines):
                Editor.TextArea.TextView.Redraw();
                break;
        }
    }

    private void EditorOnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel is not null)
        {
            _lineNumbers.InvalidateMeasure();
            _ignoreTextChange = true;
            try
            {
                ViewModel.Content = Editor.Text;
            }
            finally
            {
                _ignoreTextChange = false;
            }
        }
    }

    internal AssemblerFileViewModel? ViewModel => (AssemblerFileViewModel?)base.DataContext;
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (this.TryFindResource("LineNumber", out object? res) && res is ISolidColorBrush brush)
        {
            _lineNumberForeground = brush;
            _lineNumbers.Foreground = _lineNumberForeground;
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