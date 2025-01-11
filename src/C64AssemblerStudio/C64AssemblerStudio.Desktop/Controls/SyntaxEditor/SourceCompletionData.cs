using System.Diagnostics;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;

namespace C64AssemblerStudio.Desktop.Controls.SyntaxEditor;

public abstract class SourceCompletionData<T>: ICompletionData
    where T: IEditorCompletionItem
{
    public IImage Image => null!;
    public string Text => Item.Text;
    public object Content => Item;
    public object Description => "Description";
    public double Priority => (double)Item.Suggestion.Priority;
    protected T Item { get; }

    protected SourceCompletionData(T item)
    {
        Item = item;
    }

    public abstract void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs);
}

public class FileReferenceSourceCompletionData : SourceCompletionData<FileReferenceCompletionItem>
{
    public FileReferenceSourceCompletionData(FileReferenceCompletionItem item) : base(item)
    {
    }
    public override void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var suggestion = Item.Suggestion;
        int start = completionSegment.Offset - Item.ReplacementLength;
        string replacementText = $"{Item.PrependText}{suggestion.Text}{Item.AppendText}";
        textArea.Document.Replace(start, Item.ReplacementLength, replacementText);
        Debug.WriteLine($"Replacement length: {Item.ReplacementLength}");
    }
}
public class DirectoryReferenceSourceCompletionData : SourceCompletionData<DirectoryReferenceCompletionItem>
{
    public DirectoryReferenceSourceCompletionData(DirectoryReferenceCompletionItem item) : base(item)
    {
    }
    public override void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var suggestion = Item.Suggestion;
        int start = completionSegment.Offset - Item.ReplacementLength; // + Item.ReplacementRelativeOffset;
        string replacementText = $"{Item.PrependText}{suggestion.Text}"; // don't append for directory suggestions
        textArea.Document.Replace(start, Item.ReplacementLength, replacementText);
        Debug.WriteLine($"Replacement length: {Item.ReplacementLength}");
    }
}

public class StandardCompletionData: SourceCompletionData<StandardCompletionItem>
{
    public StandardCompletionData(StandardCompletionItem item) : base(item)
    {
    }
    public override void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var suggestion = Item.Suggestion;
        int start = completionSegment.Offset - Item.ReplacementLength; // + Item.ReplacementRelativeOffset;
        string replacementText = $"{Item.PrependText}{suggestion.Text}{Item.AppendText}";
        textArea.Document.Replace(start, Item.ReplacementLength, replacementText);
        Debug.WriteLine($"Replacement length: {Item.ReplacementLength}");
    }
}