using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class NavigationManager : INavigationManager
{
    private readonly ILogger<NavigationManager> _logger;
    private readonly IDockFactory _factory;

    public NavigationManager(IDockFactory factory, ILogger<NavigationManager> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public void Navigate(Navigation target)
    {
        var id = target.ToString();
        var cmd = _factory.RootDock.Navigate;
        if (cmd.CanExecute(id))
        {
            cmd.Execute(id);
        }
    }
}