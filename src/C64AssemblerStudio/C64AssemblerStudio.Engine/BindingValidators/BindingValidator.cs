using System.Diagnostics.CodeAnalysis;
using C64AssemblerStudio.Core;
using PropertyChanged;

namespace C64AssemblerStudio.Engine.BindingValidators;
public abstract class BindingValidator : NotifiableObject, IBindingValidator
{
    public string SourcePropertyName { get; }
    public event EventHandler? HasErrorsChanged;
    public ImmutableArray<string> Errors { get; protected set; } = ImmutableArray<string>.Empty;
    public bool HasErrors => !Errors.IsDefaultOrEmpty;
    public string? Text { get; protected set; }
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
    

    protected void ClearError()
    {
        if (HasErrors)
        {
            Errors = ImmutableArray<string>.Empty;
        }   
    }

    protected void SetError(string errorText)
    {
        Errors = ImmutableArray<string>.Empty.Add(errorText);
    }

    public virtual void Update(string? text)
    {
        Text = text;
    }
}
