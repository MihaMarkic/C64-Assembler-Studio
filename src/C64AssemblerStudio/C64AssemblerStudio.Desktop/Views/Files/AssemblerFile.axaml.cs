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
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Desktop.Controls.SyntaxEditor;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.ViewModels.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using TextMateSharp.Grammars;

namespace C64AssemblerStudio.Desktop.Views.Files;

public partial class AssemblerFile : UserControl
{
    // private readonly LineNumbers _lineNumbers;
    private SyntaxColorizer? _syntaxColorizer;
    // private ISolidColorBrush? _lineNumberForeground;
    private AssemblerFileViewModel? _oldViewModel;
    private BreakpointsMargin? _breakpointsMargin;
    private readonly MarkerRenderer _markerRenderer;
    private readonly ILogger<AssemblerFile> _logger;

    public AssemblerFile(): this(IoC.Host.Services.GetRequiredService<ILogger<AssemblerFile>>())
    {
    }
    public AssemblerFile(ILogger<AssemblerFile> logger)
    {
        _logger = logger;
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
        _markerRenderer = new();
        Editor.TextArea.TextView.BackgroundRenderers.Add(_markerRenderer);
    }

    private void CaretOnPositionChanged(object? sender, EventArgs e)
    {
        UpdateCurrentLine();
    }

    private void UpdateCurrentLine()
    {
        if (ViewModel is not null)
        {
            ViewModel.CaretLine = Editor.TextArea.Caret.Line;
            ViewModel.CaretColumn = Editor.TextArea.Caret.Column;
        }
    }

    void DetachOldViewModel()
    {
        if (_oldViewModel is not null)
        {
            _oldViewModel.PropertyChanged -= FileViewModelOnPropertyChanged;
            _oldViewModel.MoveCaretRequest -= FileViewModelOnMoveCaretRequest;
            _oldViewModel.SyntaxColoringUpdated += FileViewModelOnSyntaxColoringUpdated;
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
            fileViewModel.SyntaxColoringUpdated += FileViewModelOnSyntaxColoringUpdated;
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

    private void FileViewModelOnSyntaxColoringUpdated(object? sender, EventArgs e)
    {
        Debug.WriteLine("Redrawing lines");
        _markerRenderer.Markers.Clear();
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            _logger.LogError($"{nameof(FileViewModelOnSyntaxColoringUpdated)} invoked without a ViewModel");
        }
        else
        {
            var flatErrors = viewModel.Errors.SelectMany(l => l.Value.Items);
            _markerRenderer.Markers.AddRange(
                flatErrors
                    .Where(i => i.Range.IsClosed)
                    .Select(e => new ZigzagMarker
                    {
                        StartOffset = e.Offset ?? FindOffset(e.Line, e.Range.Start), 
                        Length = e.Range.IsClosed ? e.Range.Length: FindLength(e.Line),
                    })
                );
        }
        Editor.TextArea.TextView.Redraw();
    }

    int FindOffset(int line, int? column)
    {
        var documentLine = Editor.Document.Lines[line];
        return documentLine.Offset + (column ?? 0);
    }
    int FindLength(int line)
    {
        var documentLine = Editor.Document.Lines[line];
        return documentLine.Length;
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