using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Utils;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.ViewModels.Files;
using TextMateSharp.Grammars;

namespace C64AssemblerStudio.Desktop.Views.Files;

public partial class AssemblerFile : UserControl
{
    // private readonly LineNumbers _lineNumbers;
    private SyntaxColorizer? _syntaxColorizer;
    // private ISolidColorBrush? _lineNumberForeground;
    private AssemblerFileViewModel? _oldViewModel;
    private BreakpointsMargin? _breakpointsMargin;

    public AssemblerFile()
    {
        InitializeComponent();
        var leftMargins = Editor.TextArea.LeftMargins;
        // _lineNumbers = new LineNumbers(Editor.FontFamily, Editor.FontSize)
        // {
        //     Foreground = _lineNumberForeground,
        //     Margin = new Thickness(4, 0),
        // };
        // leftMargins.Add(_lineNumbers);
        Editor.TextChanged += EditorOnTextChanged;
        Editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;
    }

    private void CaretOnPositionChanged(object? sender, EventArgs e)
    {
        UpdateCurrentLine();
    }

    private void UpdateCurrentLine()
    {
        var currentLine = Editor.Document.GetLineByOffset(Editor.CaretOffset).LineNumber;
        if (ViewModel is not null)
        {
            ViewModel.CaretRow = currentLine - 1;
        }
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
            _syntaxColorizer = new SyntaxColorizer(fileViewModel)
            {
                ExecutionLine = fileViewModel.ExecutionLineRange
            };
            Editor.TextArea.TextView.LineTransformers.Add(_syntaxColorizer);
            fileViewModel.PropertyChanged += FileViewModelOnPropertyChanged;
            fileViewModel.MoveCaretRequest += FileViewModelOnMoveCaretRequest;
            Editor.Text = fileViewModel.Content;
            _breakpointsMargin = new BreakpointsMargin(fileViewModel);
            Editor.TextArea.LeftMargins.Add(_breakpointsMargin);
            UpdateCurrentLine();
            _oldViewModel = fileViewModel;
        }
        else
        {
            if (_syntaxColorizer is not null)
            {
                Editor.TextArea.TextView.LineTransformers.Remove(_syntaxColorizer);
            }

            if (_breakpointsMargin is not null)
            {
                Editor.TextArea.LeftMargins.Remove(_breakpointsMargin);
            }
            _breakpointsMargin = null;
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
            case nameof(AssemblerFileViewModel.Errors):
                Editor.TextArea.TextView.Redraw();
                break;
            case nameof(AssemblerFileViewModel.ExecutionLineRange):
                if (_syntaxColorizer is not null)
                {
                    var range = ViewModel.ValueOrThrow().ExecutionLineRange;
                    _syntaxColorizer.ExecutionLine = range;
                    Editor.TextArea.TextView.Redraw();
                    if (range is not null)
                    {
                        Editor.ScrollTo(range.Value.Start-1, 1);
                    }
                }
                break;
            case nameof(AssemblerFileViewModel.CallStackItems):
                if (_syntaxColorizer is not null)
                {
                    _syntaxColorizer.CreateCallStackLineNumberMap();
                    Editor.TextArea.TextView.Redraw();
                }
                break;
        }
    }

    private void EditorOnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel is not null)
        {
            // _lineNumbers.InvalidateMeasure();
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

    // protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    // {
    //     base.OnAttachedToLogicalTree(e);
    //     if (this.TryFindResource("LineNumber", out object? res) && res is ISolidColorBrush brush)
    //     {
    //         _lineNumberForeground = brush;
    //         _lineNumbers.Foreground = _lineNumberForeground;
    //     }
    // }

    private void Editor_PointerHover(object? sender, PointerEventArgs e)
    {
        if (ViewModel is not null)
        {
            var flyout = (Flyout?)FlyoutBase.GetAttachedFlyout(Editor);
            if (flyout is not null)
            {
                object? symbolReference = GetSymbolReferenceAtPosition(e);
                if (symbolReference is not null)
                {
                        FlyoutContent.DataContext = symbolReference;
                        flyout.ShowAt(Editor, true);
                }
            }
        }
    }
    SyntaxError? GetSymbolReferenceAtPosition(PointerEventArgs e)
    {
        if (ViewModel is not null)
        {
            var textView = Editor.TextArea.TextView;
            var pos = e.GetPosition(textView);
            pos = new Point(pos.X, pos.Y.CoerceValue(0, textView.Bounds.Height) + textView.VerticalOffset);
            var vp = textView.GetPosition(pos);
            if (vp is not null)
            {
                Debug.WriteLine($"Pos {vp.Value.Line} {vp.Value.Column}");
                return ViewModel.GetSyntaxErrorAt(vp.Value.Line, vp.Value.Column-1);
            }
        }
        return null;
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