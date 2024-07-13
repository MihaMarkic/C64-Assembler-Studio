using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public interface IProjectViewModel
{
    string? Path { get; set; }
}
public abstract class ProjectViewModel<TConfiguration>: ViewModel, IProjectViewModel
    where TConfiguration : Project
{
    private readonly ILogger<ProjectViewModel<TConfiguration>> _logger;
    public TConfiguration? Configuration { get; private set; }
    public string? Path { get; set; }

    public ProjectViewModel(ILogger<ProjectViewModel<TConfiguration>> logger)
    {
        _logger = logger;
    }

    public void Init(TConfiguration configuration, string? path)
    {
        Configuration = configuration;
        Path = path;
    }
}

public class EmptyProjectViewModel : ProjectViewModel<EmptyProject>
{
    public EmptyProjectViewModel(ILogger<ProjectViewModel<EmptyProject>> logger) : base(logger)
    {
    }
}

public class KickAssProjectViewModel(ILogger<KickAssProjectViewModel> logger) :
    ProjectViewModel<KickAssProject>(logger)
{
}