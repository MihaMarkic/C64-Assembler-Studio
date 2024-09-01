using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Engine.BindingValidators;

public class BreakpointConditionsValidator: BindingValidator
{
    private readonly IBreakpointConditionGrammarService _grammarService;
    public string? TextValue { get; protected set; }
    public ImmutableArray<SyntaxEditorError> GrammarErrors { get; private set; } = [];
    public ImmutableArray<SyntaxEditorToken> Tokens { get; private set; } = [];
    public BreakpointConditionsValidator(IBreakpointConditionGrammarService grammarService, string sourcePropertyName) : base(sourcePropertyName)
    {
        _grammarService = grammarService;
    }

    private CancellationTokenSource? _updateStatusCts;
    public void Update(string? text)
    {
        TextValue = text;
        _updateStatusCts?.Cancel();
        _updateStatusCts = new();
        _ = UpdateStatusAsync(text, _updateStatusCts.Token);
    }

    internal async Task UpdateStatusAsync(string? text, CancellationToken ct = default)
    {
        try
        {
            // delays for 0.2s to avoid abusing CPU when user is typing
            await Task.Delay(200, ct);
            var (hasErrors, errors, tokens) = await _grammarService.VerifyTextAsync(text, ct);
            ct.ThrowIfCancellationRequested();
            UpdateError(hasErrors,
                errors.IsEmpty
                    ? "Unknown error"
                    : string.Join(Environment.NewLine, errors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));
            GrammarErrors = errors;
            Tokens = tokens;
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }
    }

    void UpdateError(bool hasError, string errorText)
    {
        if (HasErrors && !hasError)
        {
            Errors = [];
        }
        else if (!HasErrors && hasError)
        {
            Errors = ImmutableArray<string>.Empty.Add(errorText);
        }
    }
}