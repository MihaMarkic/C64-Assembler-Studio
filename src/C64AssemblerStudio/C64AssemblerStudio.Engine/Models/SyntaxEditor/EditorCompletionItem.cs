using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace C64AssemblerStudio.Engine.Models.SyntaxEditor;

public interface IEditorCompletionItem
{
    string Text { get; }
    string Description { get; }
    Suggestion Suggestion { get; }
}
/// <summary>
/// A editor completion item.
/// </summary>
/// <param name="Priority"></param>
/// <param name="RootText">Text left of cursor for filtering the suggestions</param>
/// <param name="ReplacementLength">Length of the replacement segment</param>
public abstract record EditorCompletionItem<T>(string RootText, int ReplacementLength, string PrependText, string AppendText, T Suggestion): IEditorCompletionItem
    where T: Suggestion
{
    /// <summary>
    /// Text to be displayed in suggestions list.
    /// </summary>
    public abstract string Text { get; }
    public abstract string Description { get; }
    Suggestion IEditorCompletionItem.Suggestion => Suggestion;
    public string Source => Suggestion.Origin.ToString();
}

public record StandardCompletionItem(string RootText, int ReplacementLength, string PrependText, string AppendText, Suggestion Suggestion)
    : EditorCompletionItem<Suggestion>(RootText, ReplacementLength, PrependText, AppendText, Suggestion)
{
    public override string Text => Suggestion.Text;
    public override string Description => $"Inserts {Suggestion.Origin}";
}

public record FileReferenceCompletionItem(string RootText, int ReplacementLength, string PrependText, string AppendText, FileSuggestion Suggestion)
    : EditorCompletionItem<FileSuggestion>(RootText, ReplacementLength, PrependText, AppendText, Suggestion)
{
    public override string Text => Path.GetFileName(Suggestion.Text);
    public override string Description => $"Inserts reference to file {Suggestion.Text}";
}
public record DirectoryReferenceCompletionItem(string RootText, int ReplacementLength, string PrependText, string AppendText, DirectorySuggestion Suggestion)
    : EditorCompletionItem<DirectorySuggestion>(RootText, ReplacementLength, PrependText, AppendText, Suggestion)
{
    public override string Text => Suggestion.Text;
    public override string Description => $"Inserts reference to directory {Suggestion.Text}";
}