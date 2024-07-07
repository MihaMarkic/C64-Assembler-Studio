using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public abstract class ProjectViewModel : ViewModel
{
    readonly ILogger<ProjectViewModel> _logger;
    public string? Path { get; set; }
    public string Name { get; set; }
    public ProjectViewModel(ILogger<ProjectViewModel> logger)
    {
        _logger = logger;
        Name = string.Empty;
    }
}

public  class EmptyProjectViewModel : ProjectViewModel
{
    public EmptyProjectViewModel(ILogger<ProjectViewModel> logger) : base(logger)
    {
        Name = "Empty";
    }
}

public class KickAssProjectViewModel(ILogger<KickAssProjectViewModel> _logger): ProjectViewModel(_logger)
{
}
