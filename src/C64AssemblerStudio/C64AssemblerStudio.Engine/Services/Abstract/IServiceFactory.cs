using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Implementation;

namespace C64AssemblerStudio.Engine.Services.Abstract;

/// <summary>
/// Provides a service factory for services requiring passing arguments through constructor.
/// </summary>
public interface IServiceFactory
{
    IProjectFileWatcher CreateProjectFileWatcher(ProjectRootDirectory rootDirectory);
}