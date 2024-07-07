namespace C64AssemblerStudio.Engine.BindingValidators;

public interface IBindingValidator
{
    string SourcePropertyName { get; }
    event EventHandler? HasErrorsChanged;
    ImmutableArray<string> Errors { get; }
    bool HasErrors { get; }
    void Clear();
}
