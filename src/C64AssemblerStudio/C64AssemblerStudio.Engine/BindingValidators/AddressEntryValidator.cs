using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Engine.BindingValidators;

public class AddressEntryValidator : BindingValidator
{
    private readonly IAddressEntryGrammarService _grammarService;
    public bool IsMandatory { get; }

    public AddressEntryValidator(IAddressEntryGrammarService grammarService, string sourcePropertyName,
        bool isMandatory) : base(sourcePropertyName)
    {
        _grammarService = grammarService;
        IsMandatory = isMandatory;
    }

    public override void Update(string? text)
    {
        var (hasErrors, errorText) = _grammarService.VerifyText(text);
        if (hasErrors)
        {
            SetError(errorText.IsEmpty
                ? "Unknown error"
                : string.Join(Environment.NewLine, errorText.Select(e => $"{e.Line}:{e.Column} {e.Message}")));
        }
        else
        {
            ClearError();
        }
        base.Update(text);
    }
}