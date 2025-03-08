using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ProjectFilesWatcherViewModel: ViewModel
{
    private readonly ILogger<ProjectFilesWatcherViewModel> _logger;
    private readonly IServiceFactory _serviceFactory;
    private readonly IOSDependent _osDependent;
    private readonly IDirectoryService _directoryService;
    /// <summary>
    /// Keeps tracks of file watchers.
    /// </summary>
    private readonly Dictionary<ProjectDirectory, IProjectFileWatcher> _projectFileWatchers = new();
    private readonly Globals _globals;
    private readonly Settings _settings;
    public bool IsRefreshing { get; private set; }
    public bool IsProjectChanging { get; private set; }
    public bool IsProjectOpen { get; private set; }
    public ObservableCollection<ProjectItem> Items => Root?.Items ?? [];
    public ProjectLibraries? Libraries { get; private set; }
    public ProjectRoot? Root { get; private set; }
    private readonly ISubscription _projectChangedSubscription;
    public ProjectFilesWatcherViewModel(ILogger<ProjectFilesWatcherViewModel> logger, IServiceFactory serviceFactory,
        Globals globals, IDispatcher dispatcher, IOSDependent osDependent, IDirectoryService directoryService)
    {
        _logger = logger;
        _serviceFactory = serviceFactory;
        _globals = globals;
        _settings = globals.Settings;
        _osDependent = osDependent;
        _directoryService = directoryService;
        _globals.PropertyChanged += GlobalsOnPropertyChanged;
        _settings.PropertyChanged += SettingsOnPropertyChanged;
        _projectChangedSubscription = dispatcher.Subscribe<ProjectSettingsChangedMessage>(ProjectSettingsChanged);
        _ = RefreshAsync();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = SyncLibrariesWithProjectAsync(_settings.Libraries, CancellationToken.None);
    }

    private void GlobalsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                _ = ProjectChanged(_globals.Project);
                break;
        }
    }

    private Task ProjectSettingsChanged(ProjectSettingsChangedMessage message, CancellationToken ct)
    {
        // do nothing for now
        return Task.CompletedTask;
    }

    private record LibraryInternal(string Name, string Path);
    private async Task SyncLibrariesWithProjectAsync(FrozenDictionary<string, Library> allLibraries, CancellationToken ct)
    {
        bool hasChanges = false;
        if (Libraries is not null)
        {
            var libraries = Libraries.Items.ToFrozenDictionary(l => new LibraryInternal(l.Name, l.AbsolutePath),
                l => l);
            var newLibraries = allLibraries.Select(l => new LibraryInternal(l.Key, l.Value.Path))
                .ToHashSet();
            // first remove old ones
            foreach (var l in libraries)
            {
                if (!newLibraries.Contains(l.Key))
                {
                    hasChanges = true;
                    RemoveFileWatcher(l.Value);
                    Libraries.Items.Remove(l.Value);
                    _logger.LogInformation("Removing library node [{Name}] {AbsolutePath}", l.Key.Name, l.Key.Path);
                }
                else
                {
                    newLibraries.Remove(l.Key);
                }
            }

            if (newLibraries.Count > 0)
            {
                hasChanges = true;
                // adds remaining new libraries
                await AddNewLibrariesAsync(newLibraries, ct);
                _logger.LogInformation("Adding these new libraries {Libraries}", string.Join(", ", newLibraries.Select(l => $"[{l.Name}] on {l.Path}")));
            }

            if (hasChanges)
            {
                EnableLibraryWatchers(Libraries.ValueOrThrow().Items.ToImmutableArray());
            }
        }
        else
        {
            _logger.LogWarning("Project changed when Libraries node is not set");
        }
    }

    private async Task AddNewLibrariesAsync(ISet<LibraryInternal> libraries, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_globals.Project.Path).ValueOrThrow();
        var tasks = libraries.Select(l => AddLibraryAsync(l.Name, l.Path, ct));
        await Task.WhenAll(tasks);
    }

    private CancellationTokenSource? _projectChangingCts;
    async Task ProjectChanged(IProjectViewModel project)
    {
        if (_projectChangingCts is not null)
        {
            await _projectChangingCts.CancelAsync();
            _projectChangingCts.Dispose();
            _projectChangingCts = null;
        }

        _projectChangingCts = new();
        var token = _projectChangingCts.Token;
        IsProjectChanging = true;
        try
        {
            IsProjectOpen = project is not EmptyProjectViewModel;
            Disengage();
            await RefreshAsync();
            if (project.Directory is not null && !token.IsCancellationRequested)
            {
                EnableDirectoryWatchers(project.Directory);
            }
        }
        finally
        {
            IsProjectChanging = false;
        }
    }
    
    private CancellationTokenSource? _refreshCts;

    public async Task RefreshAsync()
    {
        if (!IsProjectOpen)
        {
            return;
        }
        IsRefreshing = true;
        try
        {
            await _refreshCts.CancelNullableAsync();

            IsProjectOpen = _globals.Project is not EmptyProjectViewModel;
            while (Items.Count > 1)
            {
                Debug.Assert(!ReferenceEquals(Items[1], Libraries));
                Items.RemoveAt(1);
            }

            if (IsProjectOpen)
            {
                _refreshCts = new CancellationTokenSource();
                var ctInternal = _refreshCts.Token;

                var project = _globals.Project;
                var directory = Path.GetDirectoryName(project.Path).ValueOrThrow();
                // virtual hidden root
                Root = new ProjectRoot(_osDependent.FileStringComparison)
                {
                    AbsoluteRootPath = Path.GetFullPath(Path.GetDirectoryName(project.Path)!).ValueOrThrow(),
                    Name = "Hidden root",
                    Parent = null,
                };
                Libraries = new ProjectLibraries(_osDependent.FileStringComparison) { Parent = null, Name = "Libraries" }; 
                Root.Items.Add(Libraries);
                var tasks = new List<Task>(_settings.Libraries.Count + 1);
                var projectRefreshTask = RefreshProjectStructureAsync(Root, ctInternal);
                tasks.Add(projectRefreshTask);
                foreach (var library in _settings.Libraries)
                {
                    tasks.Add(AddLibraryAsync(library.Key, library.Value.Path, ctInternal));
                }

                await Task.WhenAll(tasks);
                EnableLibraryWatchers([..Libraries.ValueOrThrow().Items]);
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task AddLibraryAsync(string name, string path, CancellationToken ct)
    {
        var library = new ProjectLibrary(_osDependent.FileStringComparison)
        {
            Name = name,
            Parent = null,
            AbsoluteRootPath = path,
        };
        Libraries.ValueOrThrow().Items.Add(library);
        await RefreshProjectStructureAsync(library, ct);
    }

    public Task RemoveLibraryAsync(ProjectLibrary library, CancellationToken ct)
    {
        return Task.CompletedTask;
        // if (Libraries is not null)
        // {
        //     if (Libraries.Items.Remove(library))
        //     {
        //         var project = _globals.Project;
        //         project.Libraries.Remove();
        //         await SyncLibrariesWithProjectAsync(ct);
        //     }
        // }
    }

    private async Task RefreshProjectStructureAsync(ProjectRootDirectory rootDirectory, CancellationToken ct)
    {
        try
        {
            if (_directoryService.Exists(rootDirectory.AbsolutePath))
            {
                var newItems = await Task.Run(() =>
                {
                    var internalItems = new List<ProjectItem>();
                    foreach (var i in CollectDirectoriesAndFiles(rootDirectory, rootDirectory.AbsolutePath))
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }

                        internalItems.Add(i);
                    }

                    return internalItems;
                }, ct);
                ct.ThrowIfCancellationRequested();
                foreach (var i in newItems)
                {
                    rootDirectory.Items.Add(i);
                }
            }
            else
            {
                _logger.LogError("Directory {Directory} does not exist", rootDirectory.AbsolutePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed refresh");
        }
    }
    
    void EnableDirectoryWatchers(string rootDirectory)
    {
        AddFileWatcher(Root.ValueOrThrow());
    }

    void EnableLibraryWatchers(ImmutableArray<ProjectLibrary> libraries)
    {
        foreach (var library in libraries)
        {
            AddFileWatcher(library);
        }
    }

    private void AddFileWatcher(ProjectRootDirectory rootDirectory)
    {
        var fileWatcher =  _serviceFactory.CreateProjectFileWatcher(rootDirectory);
        _projectFileWatchers.Add(rootDirectory, fileWatcher);
    }
    private void RemoveFileWatcher(ProjectRootDirectory rootDirectory)
    {
        if (_projectFileWatchers.TryGetValue(rootDirectory, out var fileWatcher))
        {
            fileWatcher.Dispose();
            _projectFileWatchers.Remove(rootDirectory);
        }
    }

    void Disengage()
    {
        foreach (var fileWatcher in _projectFileWatchers)
        {
            fileWatcher.Value.Dispose();
        }
        _projectFileWatchers.Clear();
    }
    IEnumerable<ProjectItem> CollectDirectoriesAndFiles(ProjectDirectory parent, string path)
    {
        foreach (var d in _directoryService.GetDirectories(path))
        {
            var directory = new ProjectDirectory(_osDependent.FileStringComparison) { Name = Path.GetFileName(d).ValueOrThrow(), Parent = parent };
            foreach (var i in CollectDirectoriesAndFiles(directory, d))
            {
                directory.Items.Add(i);
            }

            yield return directory;
        }

        foreach (var f in _directoryService.GetFiles(path))
        {
            var fileType = ProjectFileClassificator.Classify(f);
            yield return new ProjectFile(_osDependent.FileStringComparison) { Name = Path.GetFileName(f), FileType = fileType, Parent = parent };
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed && disposing)
        {
            _projectChangedSubscription.Dispose();
            _globals.PropertyChanged -= GlobalsOnPropertyChanged;
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
        }
        base.Dispose(disposing);
    }
}