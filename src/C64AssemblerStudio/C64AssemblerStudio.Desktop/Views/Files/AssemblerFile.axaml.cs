using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Utils;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Desktop.Controls.SyntaxEditor;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.ViewModels.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models.Parsing;

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
    private ReferencedFileElementGenerator? _referencedFileElementGenerator;
    private CompletionWindow? _completionWindow;

    public AssemblerFile() : this(IoC.Host.Services.GetRequiredService<ILogger<AssemblerFile>>())
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
        Editor.TextArea.TextEntering += TextAreaOnTextEntering;
        Editor.KeyDown += EditorOnKeyDown;
        _markerRenderer = new();
        Editor.TextArea.TextView.BackgroundRenderers.Add(_markerRenderer);
    }

    private void EditorOnKeyDown(object? sender, KeyEventArgs e)
    {
        // Debug.WriteLine($"{e.Key} {e.KeyModifiers}");
        if (e is { Key: Key.Space, KeyModifiers: KeyModifiers.Control })
        {
            _ = TextAreaOnTextEnteringAsync(TextChangeTrigger.CompletionRequested);
            e.Handled = true;
        }
    }

    private void TextAreaOnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (e.Text is "\"" or "#" or "." or ",")
        {
            _ = TextAreaOnTextEnteringAsync(TextChangeTrigger.CharacterTyped);
        }
    }

    private async Task TextAreaOnTextEnteringAsync(TextChangeTrigger trigger)
    {
        if (ViewModel is not null)
        {
            if (_completionWindow is null)
            {
                try
                {
                    var (shouldShow, items) =
                        await ViewModel.ShouldShowCompletionAsync(trigger);
                    if (shouldShow)
                    {
                        var builder = ImmutableArray.CreateBuilder<ICompletionData>();
                        foreach (var i in items)
                        {
                            ICompletionData data = i switch
                            {
                                FileReferenceCompletionItem fileReference => new FileReferenceSourceCompletionData(
                                    fileReference),
                                StandardCompletionItem standard => new StandardCompletionData(standard),
                                _ => throw new Exception($"Not handling {i.GetType().Name}"),
                            };
                            builder.Add(data);
                        }

                        ShowCompletionSuggestions(builder.ToImmutable());
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed opening suggestions");
                }
            }
        }
    }
    
    class DirectSegment: ISegment
    {
        public int Offset { get; }
        public int Length => 0;
        public int EndOffset => Offset;

        public DirectSegment(int offset)
        {
            Offset = offset;
        }
    }

    /// <summary>
    /// Shows suggestions as completion list.
    /// </summary>
    /// <param name="suggestions"></param>
    /// <typeparam name="T"></typeparam>
    public void ShowCompletionSuggestions<T>(ImmutableArray<T> suggestions)
        where T : ICompletionData
    {
        // fire suggestion selection when there is single option
        if (suggestions.Length == 1)
        {
            var suggestion = suggestions[0];
            suggestion.Complete(Editor.TextArea, new DirectSegment(Editor.TextArea.Caret.Offset), EventArgs.Empty);
        }
        // show combo listing suggestions instead
        else
        {
            _completionWindow = new CompletionWindow(Editor.TextArea);
            _completionWindow.Closed += CompletionWindowOnClosed;
            var data = _completionWindow.CompletionList.CompletionData;
            foreach (var s in suggestions)
            {
                data.Add(s);
            }
            // _completionWindow.ExpectInsertionBeforeStart = true;
            _completionWindow.Show();
        }
    }

    private void CompletionWindowOnClosed(object? sender, EventArgs e)
    {
        _completionWindow = null;
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
            ViewModel.CaretOffset = Editor.TextArea.Caret.Offset;
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
            Editor.PointerHover += Editor_PointerHover;
            UpdateReferencedFileElementGenerator();
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
            Editor.PointerHover -= Editor_PointerHover;
        }

        base.OnDataContextChanged(e);
    }

    /// <summary>
    /// Updates referenced files to generator.
    /// </summary>
    private void UpdateReferencedFileElementGenerator()
    {
        if (_referencedFileElementGenerator is not null)
        {
            Editor.TextArea.TextView.ElementGenerators.Remove(_referencedFileElementGenerator);
        }

        var lines = ViewModel?.Lines;
        if (lines is not null)
        {
            var referencedFiles = lines.SelectMany(l =>
                l.Value.Items
                    .OfType<FileReferenceSyntaxItem>()
                    .Where(rf => rf.ReferencedFile.FullFilePath is not null));
            _referencedFileElementGenerator = new([..referencedFiles], ReferencedFileClicked);
            Editor.TextArea.TextView.ElementGenerators.Add(_referencedFileElementGenerator);
        }
        else
        {
            _referencedFileElementGenerator = null;
        }

        Editor.TextArea.TextView.Redraw();
    }

    private void ReferencedFileClicked(FileReferenceSyntaxItem item)
    {
        ViewModel?.ReferencedFileClicked(item);
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
                        Length = e.Range.IsClosed ? e.Range.Length : FindLength(e.Line),
                    })
            );
        }

        UpdateReferencedFileElementGenerator();
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
                        Editor.ScrollTo(range.Value.Start - 1, 1);
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

    private void Editor_PointerHover(object? sender, PointerEventArgs e)
    {
        if (ViewModel is not null)
        {
            object? symbolReference = GetErrorReferenceAtPosition(e);
            if (symbolReference is not null)
            {
                var flyout = (Flyout?)FlyoutBase.GetAttachedFlyout(Editor);
                if (flyout is not null)
                {
                    FlyoutContent.DataContext = symbolReference;
                    flyout.ShowAt(Editor, true);
                }
            }
        }
    }

    SyntaxError? GetErrorReferenceAtPosition(PointerEventArgs e)
    {
        if (ViewModel is not null)
        {
            var textView = Editor.TextArea.TextView;
            var pos = e.GetPosition(textView);
            pos = new Point(pos.X, pos.Y.CoerceValue(0, textView.Bounds.Height) + textView.VerticalOffset);
            var vp = textView.GetPosition(pos);
            if (vp is not null)
            {
                Debug.WriteLine($"Pos {vp.Value.Line - 1} {vp.Value.Column}");
                return ViewModel.GetSyntaxErrorAt(vp.Value.Line - 1, vp.Value.Column - 1);
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