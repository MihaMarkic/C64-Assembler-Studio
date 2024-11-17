namespace C64AssemblerStudio.Engine.Models.SyntaxEditor;

public abstract record EditorCompletionItem
{
    public double Priority { get; }
    public abstract string Text { get; }
    public abstract string Description { get; }

    protected EditorCompletionItem(double priority)
    {
        Priority = priority;
    }
}

public record FileReferenceCompletionItem(string FileName, string Source) : EditorCompletionItem(0.0)
{
    public override string Text => Path.GetFileName(FileName);
    public override string Description => $"Inserts reference to file {FileName}";
}