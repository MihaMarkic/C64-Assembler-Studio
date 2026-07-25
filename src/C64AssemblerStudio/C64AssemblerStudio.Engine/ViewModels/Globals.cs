using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Dock.Model.Controls;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public interface IGlobals: INotifyPropertyChanged
{
	IProjectViewModel Project { get; }
	Settings Settings { get; }
	bool IsProjectOpen { get; }
	Task SetProjectAsync(IProjectViewModel project, CancellationToken ct);
	Task LoadAsync(CancellationToken ct);
	void Save(IRootDock layout);
	void ResetProject();
}

public sealed class Globals: NotifiableObject, IGlobals
{
    public const string AppName = "C64 Assembler Studio";
    private readonly ILogger<Globals> _logger;
    private readonly EmptyProjectViewModel _emptyProject;
    private readonly ISettingsManager _settingsManager;

    /// <summary>
    /// Holds active project, when no project is defined it contains <see cref="EmptyProjectViewModel"/>.
    /// </summary>
    public IProjectViewModel Project { get; private set; }
    public Settings Settings { get; private set; } = new();
    public bool IsProjectOpen => Project is not EmptyProjectViewModel;

    public Globals(ILogger<Globals> logger, EmptyProjectViewModel emptyProject, ISettingsManager settingsManager)
    {
        _logger = logger;
        _emptyProject = emptyProject;
        _settingsManager = settingsManager;
        Project = emptyProject;
    }

    public async Task SetProjectAsync(IProjectViewModel project, CancellationToken ct)
    {
        if (Project is not EmptyProjectViewModel)
        {
            await Project.DisposeAsync();
        }

        Project = project;
    }

    public async Task LoadAsync(CancellationToken ct)
    {
        Stopwatch sw = Stopwatch.StartNew();
        Settings = await _settingsManager.LoadSettingsAsync(ct);
        if (sw.Elapsed.TotalSeconds < 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(1) - sw.Elapsed, ct);
        }
        _logger.LogDebug("Loaded settings");
    }

    public void Save(IRootDock layout)
    {
        try
        {
            _settingsManager.Save(Settings);
            _settingsManager.SaveTools(layout);
            _logger.LogDebug("Saved settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed saving settings");
        }
    }

    public void ResetProject()
    {
        if (Project is not EmptyProjectViewModel)
        {
            Project.Dispose();
        }
        Project = _emptyProject;    
    }
}
