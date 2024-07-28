using C64AssemblerStudio.Engine.Grammars;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IAddressEntryGrammarService
{
    (bool HasError, ImmutableArray<SyntaxError> ErrorText) VerifyText(string? text);
}