using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;

namespace C64AssemblerStudio.Desktop.Views.Dialogs.Breakpoints;

public partial class BreakpointDetails : UserControl<BreakpointDetailViewModel>
{
    public BreakpointDetails()
    {
        InitializeComponent();
    }

    private void SyntaxEditor_OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (e.Text is not null)
        {
            TryShowCompletion(e.Text, showForSpace: false);
        }
    }

    private void ConditionsEditor_OnKeyDown(object? sender, KeyEventArgs e)
    {
        // catch Ctrl+Space to open completion list
        if (e is { Key: Key.Space, KeyModifiers: KeyModifiers.Control })
        {
            string? previousChar = null;
            if (ConditionsEditor.Text?.Length > 0)
            {
                int offset = ConditionsEditor.Caret.Offset;
                if (offset > 0)
                {
                    previousChar = ConditionsEditor.Text[(offset - 1)..offset];
                }
            }
            TryShowCompletion(previousChar, showForSpace: true);
            e.Handled = true;
        }
    }

    private void TryShowCompletion(string? text, bool showForSpace)
    {
        var completionSuggestions = ViewModel?.GetCompletionSuggestions(text, showForSpace);
        if (completionSuggestions is not null)
        {
            var suggestions =
                completionSuggestions.Value.Select(s => new ConditionCompletionSuggestion(s)).ToImmutableArray();
            ConditionsEditor.ShowCompletionSuggestions(suggestions);
        }
    }
}

public class ConditionCompletionSuggestion : ICompletionData
{
    public BreakpointConditionCompletionSuggestionModel Model { get; }

    public ConditionCompletionSuggestion(BreakpointConditionCompletionSuggestionModel model)
    {
        Model = model;
    }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }

    public IImage? Image => null;
    public string Text => Model.Value;
    public object Content => Model;
    public object Description => Text;
    public double Priority => 0;
}