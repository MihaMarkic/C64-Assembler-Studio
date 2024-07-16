using System.ComponentModel;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public interface IProjectViewModel
{
    RelayCommand CloseCommand { get; }
    string? Path { get; set; }
    string? Directory => Path is not null ? System.IO.Path.GetDirectoryName(Path) : null;
    Project? Configuration { get; }
    event PropertyChangedEventHandler? PropertyChanged;
}
public abstract class ProjectViewModel<TConfiguration>: OverlayContentViewModel, IProjectViewModel
    where TConfiguration : Project
{
    private readonly ILogger<ProjectViewModel<TConfiguration>> _logger;
    private readonly ISettingsManager _settingsManager;
    public TConfiguration? Configuration { get; private set; }
    Project? IProjectViewModel.Configuration => Configuration;
    public string? Path { get; set; }

    protected ProjectViewModel(ILogger<ProjectViewModel<TConfiguration>> logger, ISettingsManager settingsManager,
        IDispatcher dispatcher) : base(dispatcher)
    {
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public void Init(TConfiguration configuration, string? path)
    {
        Configuration = configuration;
        Path = path;
    }

    protected override void Closing()
    {
        _settingsManager.Save<Project>(Configuration.ValueOrThrow(), Path.ValueOrThrow(), false);
        base.Closing();
    }
}

public class EmptyProjectViewModel : ProjectViewModel<EmptyProject>
{
    public EmptyProjectViewModel(ILogger<ProjectViewModel<EmptyProject>> logger, ISettingsManager settingsManager,
        IDispatcher dispatcher)
        : base(logger, settingsManager, dispatcher)
    {
    }
}

public class KickAssProjectViewModel : ProjectViewModel<KickAssProject>
{
    public KickAssProjectViewModel(ILogger<ProjectViewModel<KickAssProject>> logger, ISettingsManager settingsManager,
        IDispatcher dispatcher)
        : base(logger, settingsManager, dispatcher)
    {
    }
}