using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Engine.BindingValidators;
using PropertyChanged;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ErrorHandler
{
    public class Builder()
    {
        private readonly Dictionary<string, ImmutableArray<IBindingValidator>> _validators = new ();
        public Builder AddValidator(string name, IBindingValidator validator)
        {
            _validators.Add(name, [validator]);
            return this;
        }
        public ErrorHandler Build()
        {
            return new ErrorHandler(_validators);
        }
    }
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasChanges { get; set; }
    public bool HasErrors { get; set; }
    public bool CanUpdate => HasChanges && !HasErrors;
    public FrozenDictionary<string, ImmutableArray<IBindingValidator>> Validators { get; }

    public ErrorHandler(Dictionary<string, ImmutableArray<IBindingValidator>> validators)
    {
        Validators = validators.ToFrozenDictionary();
        BindValidatorsErrors();
    }
    public static ErrorHandler.Builder CreateBuilder() => new Builder();
    private void BindValidatorsErrors()
    {
        // bind all validators
        foreach (var validator in Validators.Values.SelectMany(a => a))
        {
            validator.HasErrorsChanged += ValidatorHasErrorsChanged;
        }
    }
    [SuppressPropertyChangedWarnings]
    void OnErrorsChanged(DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(this, e);
    void ValidatorHasErrorsChanged(object? sender, EventArgs e)
    {
        var validator = (IBindingValidator)sender!;
        HasErrors = Validators.Values
            .SelectMany(a => a)
            .Any(v => v.HasErrors);
        OnErrorsChanged(new DataErrorsChangedEventArgs(validator.SourcePropertyName));
    }
    

    public IEnumerable GetErrors(string? propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName) && Validators.TryGetValue(propertyName, out var propertyValidators))
        {
            var errors = new List<string>();
            foreach (var pv in propertyValidators)
            {
                errors.AddRange(pv.Errors);
            }
            HasErrors = errors.Count > 0;
            return errors.ToImmutableArray();
        }
        else
        {
            return Enumerable.Empty<string>();
        }
    }
}