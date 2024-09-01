using C64AssemblerStudio.Core;
using PropertyChanged;

namespace C64AssemblerStudio.Engine.BindingValidators;
public abstract class BindingValidator : NotifiableObject, IBindingValidator
{
    public string SourcePropertyName { get; }
    public event EventHandler? HasErrorsChanged;
    public ImmutableArray<string> Errors { get; protected set; } = ImmutableArray<string>.Empty;
    public bool HasErrors => !Errors.IsDefaultOrEmpty;

    protected BindingValidator(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }
    [SuppressPropertyChangedWarnings]
    protected virtual void OnHasErrorsChanged(EventArgs e) => HasErrorsChanged?.Invoke(this, e);
    public void Clear()
    {
        if (HasErrors)
        {
            Errors = ImmutableArray<string>.Empty;
            OnHasErrorsChanged(EventArgs.Empty);
        }
    }
    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(HasErrors):
                OnHasErrorsChanged(EventArgs.Empty);
                break;
        }
        base.OnPropertyChanged(name);
    }
}
