using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace C64AssemblerStudio.Desktop.Views.Files;

public abstract class AdditionalLineInfoMargin : AbstractMargin
{
    /// <inheritdoc/>
    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        if (oldTextView is not null)
        {
            oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
        }
        base.OnTextViewChanged(oldTextView, newTextView);
        if (newTextView is not null)
        {
            newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
        }
        InvalidateVisual();
    }
    /// <inheritdoc/>
    protected override void OnDocumentChanged(TextDocument? oldDocument, TextDocument? newDocument)
    {
        if (oldDocument is not null)
        {
            TextDocumentWeakEventManager.LineCountChanged.RemoveHandler(oldDocument, OnDocumentLineCountChanged);
        }
        base.OnDocumentChanged(oldDocument, newDocument);
        if (newDocument is not null)
        {
            TextDocumentWeakEventManager.LineCountChanged.AddHandler(newDocument, OnDocumentLineCountChanged);
        }
        OnDocumentLineCountChanged();
    }
    void OnDocumentLineCountChanged(object? sender, EventArgs e)
    {
        OnDocumentLineCountChanged();
    }

    void TextViewVisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateMeasure();
    }
    /// <summary>
    /// Maximum length of a line number, in characters
    /// </summary>
    protected int MaxLineNumberLength = 1;

    protected virtual void OnDocumentLineCountChanged()
    { }
}