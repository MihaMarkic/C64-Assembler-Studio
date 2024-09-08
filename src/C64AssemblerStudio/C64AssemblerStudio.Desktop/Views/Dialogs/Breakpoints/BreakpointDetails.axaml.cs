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
            TryShowCompletion(e.Text);
        }
    }

    private void ConditionsEditor_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e is { Key: Key.Space, KeyModifiers: KeyModifiers.Control })
        {
            Debug.WriteLine($"KeyDown on thread {Thread.CurrentThread.ManagedThreadId}");
            TryShowCompletion(null);
            e.Handled = true;
        }
    }

    private void TryShowCompletion(string? text)
    {
        Debug.WriteLine($"TryShowCompletion on thread {Thread.CurrentThread.ManagedThreadId}");
        var completionSuggestions = ViewModel?.GetCompletionSuggestions(text);
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
    public ConditionCompletionSuggestionModel Model { get; }
    public ConditionCompletionSuggestion(ConditionCompletionSuggestionModel model)
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