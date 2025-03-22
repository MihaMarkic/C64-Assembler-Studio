using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public interface INavigationManager
{
    void Navigate(Navigation target);
}