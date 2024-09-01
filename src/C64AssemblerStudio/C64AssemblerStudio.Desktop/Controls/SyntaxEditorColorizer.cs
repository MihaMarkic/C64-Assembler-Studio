using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using CommunityToolkit.Diagnostics;

namespace C64AssemblerStudio.Desktop.Controls;

public class SyntaxEditorColorizer: DocumentColorizingTransformer
{
    private static readonly TextDecorationCollection Squiggle;
    public IList<SyntaxEditorToken>? Tokens { get; set; }
    public IList<SyntaxEditorError>? Errors { get; set; }
    public Dictionary<object, SyntaxEditorFormating>? Formatters { get; set; }

    static SyntaxEditorColorizer()
    {
        Squiggle = TextDecorations.Underline;
        Squiggle.Add(new TextDecoration { StrokeThickness = 4, Stroke = Brushes.Red});
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (!line.IsDeleted)
        {
            if (Tokens?.Count > 0 && Formatters?.Count > 0)
            {
                var lineTokens = FindLineTokens(line.LineNumber);
                foreach (var token in lineTokens)
                {
                    DrawToken(line, token);
                }
            }

            if (Errors?.Count > 0)
            {
                foreach (var e in Errors)
                {
                    DrawError(line, e);
                }
            }
        }
    }
    
    internal void DrawError(DocumentLine line, SyntaxEditorError error)
    {
        var (startOffset, endOffset) = CalculateOffsetsWithinLine(line, error.Column, error.Length);
        ChangeLinePart(startOffset, endOffset, ApplySyntaxErrorChanges);
    }

    public (int StartOffset, int EndOffset) CalculateOffsetsWithinLine(DocumentLine line, int column, int length)
    {
        int lineLength = line.Length;
        int start = Math.Min(lineLength, Math.Max(0, column));
        int end = Math.Min(lineLength, column + length);
        int startOffset = line.Offset + start;
        int endOffset = line.Offset + end;
        return (startOffset, endOffset);
    }

    internal void DrawToken(DocumentLine line, SyntaxEditorToken token)
    {
        Guard.IsNotNull(Formatters);
        if (Formatters.TryGetValue(token.TokenType, out var syntaxEditorFormating))
        {
            var (startOffset, endOffset) = CalculateOffsetsWithinLine(line, token.Column, token.Length);
            ChangeLinePart(startOffset, endOffset, e => ApplyTokenChanges(syntaxEditorFormating, e));
        }
    }
    void ApplyTokenChanges(SyntaxEditorFormating formatting, VisualLineElement element)
    {
        if (formatting.ForegroundColor is not null)
        {
            element.TextRunProperties.SetForegroundBrush(formatting.ForegroundColor);
        }

        if (formatting.BackgroundColor is not null)
        {
            element.TextRunProperties.SetBackgroundBrush(formatting.BackgroundColor);
        }
    }
    void ApplySyntaxErrorChanges(VisualLineElement element)
    {
        element.TextRunProperties.SetTextDecorations(Squiggle);
    }

    internal IEnumerable<SyntaxEditorToken> FindLineTokens(int lineNumber)
    {
        if (Tokens is not null)
        {
            return Tokens.Where(t => t.Line == lineNumber);
        }

        return [];
    }
}