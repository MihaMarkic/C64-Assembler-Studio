using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace C64AssemblerStudio.Desktop.Views.Files;

public class LineNumbers : AdditionalLineInfoMargin
{
    readonly double fontSize;
    internal IBrush? Foreground { get; set; }
    readonly Typeface typeface;
    //private string? text;
    public LineNumbers(FontFamily fontFamily, double fontSize)
    {
        this.fontSize = fontSize;
        typeface = new Typeface(fontFamily);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var text = new FormattedText(
            new string('0', MaxLineNumberLength), // max address length is 4 chars
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Foreground
        );
        return new Size(text.Width, 0);
    }
    public override void Render(DrawingContext context)
    {
        var textView = TextView;
        var renderSize = Bounds.Size;
        // necessary to capture pointer
        if (textView?.VisualLinesValid == true)
        {
            foreach (var visualLine in textView.VisualLines)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
                var text = new FormattedText(
                    lineNumber.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Foreground
                );
                context.DrawText(text, new Point(renderSize.Width - text.Width, y - TextView.VerticalOffset));
            }
        }
    }

    protected override void OnDocumentLineCountChanged()
    {
        var documentLineCount = Document?.LineCount ?? 1;
        var newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

        // The margin looks too small when there is only one digit, so always reserve space for
        // at least two digits
        if (newLength < 2)
        {
            newLength = 2;
        }
        if (newLength != MaxLineNumberLength)
        {
            MaxLineNumberLength = newLength;
            InvalidateMeasure();
        }
        base.OnDocumentLineCountChanged();
    }
}