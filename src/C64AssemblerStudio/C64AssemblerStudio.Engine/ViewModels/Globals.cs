using System.Collections.Frozen;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.ViewModels;

public sealed class Globals: NotifiableObject
{
    public const string AppName = "C64 Assembler Studio";
    private readonly ILogger<Globals> _logger;
    private readonly EmptyProjectViewModel _emptyProject;
    private readonly ISettingsManager _settingsManager;
    private readonly IFileService _fileService;
    public event EventHandler? ProjectChanged;

    /// <summary>
    /// Holds active project, when no project is defined it contains <see cref="EmptyProjectViewModel"/>.
    /// </summary>
    public IProjectViewModel Project { get; private set; }

    public Settings Settings { get; private set; } = new();
    public bool IsProjectOpen => Project is not EmptyProjectViewModel;

    public Globals(ILogger<Globals> logger, EmptyProjectViewModel emptyProject, ISettingsManager settingsManager,
        IFileService fileService)
    {
        _logger = logger;
        _emptyProject = emptyProject;
        _settingsManager = settingsManager;
        Project = emptyProject;
        _fileService = fileService;
    }

    private void RaiseProjectChanged(EventArgs e) => ProjectChanged?.Invoke(this, e);

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
        if (Project is not EmptyProjectViewModel)
        {
            Project.Dispose();
        }
        Project = _emptyProject;    
    }

    /// <summary>
    /// Searches for matching files in project and all libraries.
    /// </summary>
    /// <param name="filter">Relative file path without extension or *, just the directory and file name part</param>
    /// <param name="extension">File extension to watch for.</param>
    /// <param name="excludedFile">Full file name to exclude from results</param>
    /// <returns>A dictionary with source as key and file names array as value.</returns>
    public FrozenDictionary<string, ImmutableArray<string>> GetMatchingFiles(string filter, string extension, string? excludedFile = null)
    {
        string? projectDirectory = Project.Directory;
        ArgumentNullException.ThrowIfNull(projectDirectory);
        var searchDirectory = Path.GetDirectoryName(filter) ?? string.Empty;
        string directExtension = Path.GetExtension(filter);
        string fileName = Path.GetFileNameWithoutExtension(filter);
        extension = directExtension.Length > 1 ? $"{directExtension}*" : $".{extension}";
        var searchPattern = $"{fileName}*{extension}";
        var builder = new Dictionary<string, ImmutableArray<string>>
        {
            { "Project", _fileService.GetFilteredFiles(Path.Combine(projectDirectory, searchDirectory), searchPattern, excludedFile) },
        };
        foreach (var library in Settings.Libraries)
        {
            builder.Add(library.Key,
                _fileService.GetFilteredFiles(Path.Combine(library.Value.Path, searchDirectory), searchPattern));
        }

        return builder.ToFrozenDictionary();
    }
}
