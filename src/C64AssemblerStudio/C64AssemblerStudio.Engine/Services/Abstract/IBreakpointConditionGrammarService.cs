using C64AssemblerStudio.Engine.Models.SyntaxEditor;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IBreakpointConditionGrammarService
{
    Task<(bool HasError, ImmutableArray<SyntaxEditorError> Errors, ImmutableArray<SyntaxEditorToken> Tokens)>
        VerifyTextAsync(string? text, CancellationToken ct = default);
    (bool HasError, ImmutableArray<SyntaxEditorError> Errors, ImmutableArray<SyntaxEditorToken> Tokens) VerifyText(string? text);
}