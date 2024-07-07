using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Engine.BindingValidators;
public sealed class HigherThanBindingValidator : BindingValidator
{
    public HigherThanBindingValidator(string sourcePropertyName): base(sourcePropertyName)
    { }
    public void Update(BreakpointNoBind? bind)
    {
        UpdateError(bind is not null ? bind.EndAddress <= bind.StartAddress : false);
    }

    void UpdateError(bool isError)
    {
        if (HasErrors && !isError)
        {
            Errors = ImmutableArray<string>.Empty;
        }
        else if (!HasErrors && isError)
        {
            Errors = ImmutableArray<string>.Empty.Add("End Address should be higher than Start Address");
        }
    }
}
