namespace C64AssemblerStudio.Engine.BindingValidators;

/// <summary>
/// Provides validation of bound value before it is being converted.
/// ViewModel should route a string value to <see cref="TextValue"/> property and track <see cref="HasErrorsChanged"/> event.
/// </summary>
/// <typeparam name="TSource">Source type for UI string value.</typeparam>
public abstract class StringValidator<TSource> : BindingValidator, IBindingValidator
{
    readonly Action<TSource> _assignToSource;
    public abstract string? ConvertTo(TSource source);
    protected abstract (bool IsValid, TSource Value, string? error) ConvertFrom(string? text);

    protected StringValidator(string sourcePropertyName, Action<TSource> assignToSource): base(sourcePropertyName)
    {
        _assignToSource = assignToSource;
    }
    public override void Update(string? text)
    {
        var (isValid, value, error) = ConvertFrom(text);
        if (isValid)
        {
            _assignToSource(value);
            Errors = ImmutableArray<string>.Empty;
        }
        else
        {
            Errors = ImmutableArray<string>.Empty.Add(error ?? "Unknown error");
        }
        base.Update(text);
    }
}
