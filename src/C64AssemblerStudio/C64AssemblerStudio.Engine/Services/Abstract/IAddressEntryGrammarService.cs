using System.Collections.Frozen;
using C64AssemblerStudio.Engine.Grammars;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IAddressEntryGrammarService
{
    (bool HasError, ImmutableArray<SyntaxError> ErrorText) VerifyText(string? text);
    ushort? CalculateAddress(IDictionary<string, Label> labels, string? text);
}