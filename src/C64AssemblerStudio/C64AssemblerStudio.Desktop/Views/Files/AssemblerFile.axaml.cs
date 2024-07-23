using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using AvaloniaEdit;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.ViewModels.Files;
using TextMateSharp.Grammars;

namespace C64AssemblerStudio.Desktop.Views.Files;

public partial class AssemblerFile : UserControl
{
    static readonly RegistryOptions RegistryOptions;
    static readonly PropertyInfo TextEditorScrollViewerPropertyInfo;
    readonly LineNumbers _lineNumbers;
    private SyntaxColorizer? _syntaxColorizer;
    ISolidColorBrush? _lineNumberForeground;
    private AssemblerFileViewModel? _oldViewModel;

    static AssemblerFile()
    {
        TextEditorScrollViewerPropertyInfo = typeof(TextEditor)
            .GetProperty("ScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic)
            .ValueOrThrow();
        RegistryOptions = new RegistryOptions(ThemeName.SolarizedLight);
    }

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

    void DetachOldViewModel()
    {
        if (_oldViewModel is not null)
        {
            _oldViewModel.PropertyChanged -= FileViewModelOnPropertyChanged;
            _oldViewModel.MoveCaretRequest -= FileViewModelOnMoveCaretRequest;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        DetachOldViewModel();
        if (DataContext is AssemblerFileViewModel fileViewModel)
        {
            _syntaxColorizer = new SyntaxColorizer(fileViewModel);
            Editor.TextArea.TextView.LineTransformers.Add(_syntaxColorizer);
            fileViewModel.PropertyChanged += FileViewModelOnPropertyChanged;
            fileViewModel.MoveCaretRequest += FileViewModelOnMoveCaretRequest;
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

    private async void FileViewModelOnMoveCaretRequest(object? sender, MoveCaretEventArgs e)
    {
        var caret = Editor.TextArea.Caret;
        caret.Line = e.Line;
        caret.Column = e.Column;
        if (Editor.Bounds.Height <= 0)
        {
            await Editor.WaitForLayoutUpdatedAsync();
        }
        Editor.ScrollTo(e.Line, e.Column);
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