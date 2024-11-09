using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Engine.BindingValidators;

public class BreakpointConditionsValidator: BindingValidator
{
    private readonly IBreakpointConditionGrammarService _grammarService;
    public ImmutableArray<SyntaxEditorError> GrammarErrors { get; private set; } = [];
    public ImmutableArray<SyntaxEditorToken> Tokens { get; private set; } = [];
    public BreakpointConditionsValidator(IBreakpointConditionGrammarService grammarService, string sourcePropertyName) : base(sourcePropertyName)
    {
        _grammarService = grammarService;
    }

    private CancellationTokenSource? _updateStatusCts;
    public override void Update(string? text)
    {
        base.Update(text);
        if (_updateStatusCts is not null)
        {
            _updateStatusCts.Dispose();
            _updateStatusCts.Cancel();
            _updateStatusCts = null;
        }

        _updateStatusCts = new();
        _ = UpdateStatusAsync(text, _updateStatusCts.Token);
    }

    private async Task UpdateStatusAsync(string? text, CancellationToken ct = default)
    {
        try
        {
            // delays for 0.2s to avoid abusing CPU when user is typing
            await Task.Delay(200, ct);
            var (hasErrors, errors, tokens) = await _grammarService.VerifyTextAsync(text, ct);
            ct.ThrowIfCancellationRequested();
            if (hasErrors)
            {
                SetError(errors.IsEmpty
                    ? "Unknown error"
                    : string.Join(Environment.NewLine, errors.Select(e => $"{e.Line}:{e.Column} {e.Message}")));
            }
            else
            {
                ClearError();
            }
            GrammarErrors = errors;
            Tokens = tokens;
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }
    }
}