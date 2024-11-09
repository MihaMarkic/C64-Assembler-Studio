using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class ServiceFactory : IServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IProjectFileWatcher CreateProjectFileWatcher(ProjectRootDirectory rootDirectory)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<ProjectFileWatcher>>();
        return new ProjectFileWatcher(rootDirectory, logger);
    }
}