using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;

namespace C64AssemblerStudio.Desktop.Controls.SyntaxEditor;

public abstract class SourceCompletionData<T>: ICompletionData
    where T: EditorCompletionItem
{
    public IImage Image => null!;
    public string Text => Item.Text;
    public object Content => Item;
    public object Description => "Description";
    public double Priority => Item.Priority;
    protected T Item { get; }

    protected SourceCompletionData(T item)
    {
        Item = item;
    }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        int start = completionSegment.Offset - Item.RootText.Length;
        string replacementText = Item.PostfixDoubleQuote ? $"{Text}\"" : Text;
        textArea.Document.Replace(start, Item.ReplacementLength, replacementText);
    }
}

public class FileReferenceSourceCompletionData : SourceCompletionData<FileReferenceCompletionItem>
{
    public FileReferenceSourceCompletionData(FileReferenceCompletionItem item) : base(item)
    {
    }
}