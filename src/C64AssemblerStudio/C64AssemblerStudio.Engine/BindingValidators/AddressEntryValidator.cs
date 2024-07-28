using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Engine.BindingValidators;

public class AddressEntryValidator : BindingValidator
{
    private readonly IAddressEntryGrammarService _grammarService;
    public string? TextValue { get; protected set; }
    public bool IsMandatory { get; }

    public AddressEntryValidator(IAddressEntryGrammarService grammarService, string sourcePropertyName,
        bool isMandatory) : base(sourcePropertyName)
    {
        _grammarService = grammarService;
        IsMandatory = isMandatory;
    }

    public void Update(string? text)
    {
        var (hasErrors, errorText) = _grammarService.VerifyText(text);
        UpdateError(hasErrors,
            errorText.IsEmpty
                ? "Unknown error"
                : string.Join(Environment.NewLine, errorText.Select(e => $"{e.Line}:{e.Column} {e.Message}")));
        TextValue = text;
    }

    void UpdateError(bool hasError, string errorText)
    {
        if (HasErrors && !hasError)
        {
            Errors = ImmutableArray<string>.Empty;
        }
        else if (!HasErrors && hasError)
        {
            Errors = ImmutableArray<string>.Empty.Add(errorText);
        }
    }
}