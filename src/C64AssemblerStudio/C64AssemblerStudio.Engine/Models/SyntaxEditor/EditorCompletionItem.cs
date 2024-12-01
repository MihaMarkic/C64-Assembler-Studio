namespace C64AssemblerStudio.Engine.Models.SyntaxEditor;

/// <summary>
/// A editor completion item.
/// </summary>
/// <param name="Priority"></param>
/// <param name="RootText">Text left of cursor for filtering the suggestions</param>
/// <param name="ReplacementLength">Lenght of the replacement segment</param>
public abstract record EditorCompletionItem(double Priority, string RootText, int ReplacementLength, int ReplacementRelativeOffset)
{
    /// <summary>
    /// Text to be displayed in suggestions list.
    /// </summary>
    public abstract string Text { get; }
    public abstract string Description { get; }
    /// <summary>
    /// Whether text insertion should add double quotes at the end. Used for file references.
    /// </summary>
    public bool PostfixDoubleQuote { get; init; }
}

public record StandardCompletionItem(
    string ReplacementText,
    string Source,
    string RootText,
    int ReplacementLength,
    int ReplacementRelativeOffset)
    : EditorCompletionItem(0.0, RootText, ReplacementLength, ReplacementRelativeOffset)
{
    public override string Text => ReplacementText;
    public override string Description => $"Inserts {Source}";
}
    

/// <summary>
/// A editor completion item for file references.
/// </summary>
/// <param name="FileName">Name of the file</param>
/// <param name="Source">File source - Project or Library</param>
/// <param name="RootText">Text left of cursor for filtering the suggestions</param>
/// <param name="ReplacementLength">Lenght of the replacement segment</param>
public record FileReferenceCompletionItem(string FileName, string Source, string RootText, int ReplacementLength)
    : EditorCompletionItem(0.0, RootText, ReplacementLength, 0)
{
    public override string Text => Path.GetFileName(FileName);
    public override string Description => $"Inserts reference to file {FileName}";
}