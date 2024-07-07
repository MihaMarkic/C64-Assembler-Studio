using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Engine.ViewModels;

public abstract class ScopedViewModel : ViewModel
{
    public IServiceScope? Scope { get; private set; }
    internal void AssignScope(IServiceScope scope)
    {
        Scope = scope;
    }
}