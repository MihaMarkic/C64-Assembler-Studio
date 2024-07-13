using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public class Globals: NotifiableObject
{
    public const string AppName = "C64 Assembler Studio";
    readonly ILogger<Globals> _logger;
    readonly EmptyProjectViewModel _emptyProject;
    readonly ISettingsManager _settingsManager;
    /// <summary>
    /// Holds active project, when no project is defined it contains <see cref="EmptyProjectViewModel"/>.
    /// </summary>
    public IProjectViewModel Project { get; set; }
    public Settings Settings { get; set; } = new Settings();
    public Globals(ILogger<Globals> logger, EmptyProjectViewModel emptyProject, ISettingsManager settingsManager)
    {
        _logger = logger;
        this._emptyProject = emptyProject;
        this._settingsManager = settingsManager;
        Project= emptyProject;
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

    public void Save()
    {
        try
        {
            _settingsManager.Save(Settings);
            _logger.LogDebug("Saved settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed saving settings");
        }
    }

    public void ResetProject()
    {
        Project = _emptyProject;    
    }
}
