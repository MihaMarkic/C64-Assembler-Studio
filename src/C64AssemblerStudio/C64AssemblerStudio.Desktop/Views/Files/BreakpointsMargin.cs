using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Desktop.Views.Files;

public class BreakpointsMargin : AdditionalLineInfoMargin
{
    // diameter of breakpoint icon
    private const double D = 14;
    private static readonly IBrush ActiveBrush = Brushes.Red;
    private static readonly IBrush HoverBrush = Brushes.LightGray;
    private readonly AssemblerFileViewModel _sourceFileViewModel;
    private int? _hoverLine;
    public BreakpointsMargin(AssemblerFileViewModel sourceFileViewModel)
    {
        _sourceFileViewModel = sourceFileViewModel;
        _sourceFileViewModel.PropertyChanged += SourceFileViewModel_PropertyChanged;
        _sourceFileViewModel.BreakpointsChanged += SourceFileViewModel_BreakpointsChanged;
        Margin = new Thickness(4, 0);
    }

    void SourceFileViewModel_BreakpointsChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    void SourceFileViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AssemblerFileViewModel.Lines):
                InvalidateVisual();
                break;
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(D, 0);
    }
    /// <inheritdoc/>
    public override void Render(DrawingContext drawingContext)
    {
        var textView = TextView;
        var renderSize = Bounds.Size;
        // necessary to capture pointer
        drawingContext.FillRectangle(Brushes.Transparent, new Rect(new Point(0, 0), renderSize));
        if (textView is { VisualLinesValid: true })
        {
            foreach (var visualLine in textView.VisualLines)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                if (_sourceFileViewModel.HasBreakPointAtLine(lineNumber - 1))
                {
                    DrawBreakpointIcon(drawingContext, ActiveBrush, visualLine);
                }
                else if (lineNumber == _hoverLine)
                {
                    DrawBreakpointIcon(drawingContext, HoverBrush, visualLine);
                }
            }
        }
    }

    void DrawBreakpointIcon(DrawingContext drawingContext, IBrush brush, VisualLine visualLine)
    {
        var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextMiddle);
        drawingContext.DrawGeometry(brush, null, new EllipseGeometry(new Rect(0, y - D / 2 - TextView.VerticalOffset, D, D)));
    }

    protected override async void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled && TextView is not null && TextArea is not null)
        {
            var visualLine = GetTextLineSegment(e);
            if (visualLine is not null)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                await _sourceFileViewModel.AddOrRemoveBreakpoint(lineNumber - 1);
            }
        }
        e.Handled = true;
    }
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        UpdateHoverPosition(e);
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        UpdateHoverPosition(e);
        var cursorSet = false;
        if (!e.Handled && TextView is not null && TextArea is not null)
        {
            var visualLine = GetTextLineSegment(e);
            if (visualLine is not null)
            {
                // var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                Cursor = new Cursor(StandardCursorType.Arrow);
                cursorSet = true;
            }
        }
        if (!cursorSet)
        {
            Cursor = new Cursor(StandardCursorType.Ibeam);
        }
    }
    void UpdateHoverPosition(PointerEventArgs e)
    {
        if (!e.Handled && TextView is not null && TextArea is not null)
        {
            var visualLine = GetTextLineSegment(e);
            if (visualLine is not null)
            {
                var newHoverLine = visualLine.FirstDocumentLine.LineNumber;
                if (newHoverLine != _hoverLine)
                {
                    _hoverLine = newHoverLine;
                    InvalidateVisual();
                }
            }
        }
    }
    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoverLine = null;
        base.OnPointerExited(e);
        InvalidateVisual();
    }
    public VisualLine? GetTextLineSegment(PointerEventArgs e)
    {
        var pos = e.GetPosition(TextView);
        pos = new Point(0, pos.Y.CoerceValue(0, TextView.Bounds.Height) + TextView.VerticalOffset);
        var vl = TextView.GetVisualLineFromVisualTop(pos.Y);
        return vl;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _sourceFileViewModel.PropertyChanged -= SourceFileViewModel_PropertyChanged;
        _sourceFileViewModel.BreakpointsChanged -= SourceFileViewModel_BreakpointsChanged;
        base.OnDetachedFromVisualTree(e);
    }
}