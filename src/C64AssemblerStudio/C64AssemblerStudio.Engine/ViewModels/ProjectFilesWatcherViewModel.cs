using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ProjectFilesWatcherViewModel: ViewModel
{
    private readonly ILogger<ProjectFilesWatcherViewModel> _logger;
    private readonly IServiceFactory _serviceFactory;
    /// <summary>
    /// Keeps tracks of file watchers.
    /// </summary>
    private readonly Dictionary<ProjectDirectory, IProjectFileWatcher> _projectFileWatchers = new();
    private readonly Globals _globals;
    public bool IsRefreshing { get; private set; }
    public bool IsProjectChanging { get; private set; }
    public bool IsProjectOpen { get; private set; }
    public ObservableCollection<ProjectItem> Items => Root?.Items ?? [];
    public ProjectLibraries? Libraries { get; private set; }
    public ProjectRoot? Root { get; private set; }
    private readonly ISubscription _projectChangedSubscription;
    public ProjectFilesWatcherViewModel(ILogger<ProjectFilesWatcherViewModel> logger, IServiceFactory serviceFactory,
        Globals globals, IDispatcher dispatcher)
    {
        _logger = logger;
        _serviceFactory = serviceFactory;
        _globals = globals;
        _globals.PropertyChanged += GlobalsOnPropertyChanged;
        _projectChangedSubscription = dispatcher.Subscribe<ProjectChangedMessage>(ProjectChanged);
        _ = RefreshAsync();
    }
    private async void GlobalsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                _ = ProjectChanged(_globals.Project);
                break;
        }
    }

    private async Task ProjectChanged(ProjectChangedMessage message, CancellationToken ct)
    {
        await SyncLibrariesWithProjectAsync(ct);
    }

    private async Task SyncLibrariesWithProjectAsync(CancellationToken ct)
    {
        if (Libraries is not null)
        {
            var libraries = Libraries.Items.ToFrozenDictionary(l => l.AbsolutePath, OsDependent.FileStringComparer);
            var newLibraries = _globals.Project.Libraries.ToHashSet(OsDependent.FileStringComparer);
            // first remove old ones
            foreach (var l in libraries)
            {
                if (!newLibraries.Contains(l.Key))
                {
                    Libraries.Items.Remove(l.Value);
                    _logger.LogInformation("Removing library node {AbsolutePath}", l.Key);
                }
                else
                {
                    newLibraries.Remove(l.Key);
                }
            }

            if (newLibraries.Count > 0)
            {
                // adds remaining new libraries
                await AddNewLibrariesAsync(newLibraries, ct);
                _logger.LogInformation("Adding these new libraries {Libraries}", string.Join(", ", newLibraries));
            }
        }
        else
        {
            _logger.LogWarning("Project changed when Libraries node is not set");
        }
    }

    private async Task AddNewLibrariesAsync(ISet<string> libraries, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_globals.Project.Path).ValueOrThrow();
        var tasks = libraries.Select(l => AddLibraryAsync(directory, l, ct));
        await Task.WhenAll(tasks);
    }

    private CancellationTokenSource? _projectChangingCts;
    async Task ProjectChanged(IProjectViewModel project)
    {
        await _projectChangingCts.CancelNullableAsync();
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
                EnableDirectoryWatchers(project.Directory, project.Libraries);
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
                Root = new ProjectRoot
                {
                    AbsoluteRootPath = Path.GetFullPath(Path.GetDirectoryName(project.Path)!).ValueOrThrow(),
                    Name = "Hidden root",
                    Parent = null,
                };
                Libraries = new ProjectLibraries { Parent = null, Name = "Libraries" }; 
                Root.Items.Add(Libraries);
                var tasks = new List<Task>(project.Libraries.Length + 1);
                var projectRefreshTask = RefreshProjectStructureAsync(Root, ctInternal);
                tasks.Add(projectRefreshTask);
                foreach (var libraryPath in project.Libraries)
                {
                    tasks.Add(AddLibraryAsync(directory, libraryPath, ctInternal));
                }

                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task AddLibraryAsync(string projectDirectory, string relativePath, CancellationToken ct)
    {
        string absoluteLibraryPath = Path.GetFullPath(Path.Combine(projectDirectory, relativePath));
        var library = new ProjectLibrary
        {
            Name = Path.GetFileNameWithoutExtension(relativePath),
            Parent = null,
            AbsoluteRootPath = absoluteLibraryPath,
        };
        Libraries.ValueOrThrow().Items.Add(library);
        await RefreshProjectStructureAsync(library, ct);
    }

    private async Task RefreshProjectStructureAsync(ProjectRootDirectory rootDirectory, CancellationToken ct)
    {
        try
        {
            if (Directory.Exists(rootDirectory.AbsolutePath))
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
    
    void EnableDirectoryWatchers(string rootDirectory, ImmutableArray<string> libraries)
    {
        AddFileWatcher(Root.ValueOrThrow());
        foreach (var library in Libraries.ValueOrThrow().Items)
        {
            AddFileWatcher(library);
        }
    }

    private void AddFileWatcher(ProjectRootDirectory rootDirectory)
    {
        var fileWatcher =  _serviceFactory.CreateProjectFileWatcher(rootDirectory);
        _projectFileWatchers.Add(rootDirectory, fileWatcher);
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
        foreach (var d in Directory.GetDirectories(path))
        {
            var directory = new ProjectDirectory { Name = Path.GetFileName(d).ValueOrThrow(), Parent = parent };
            foreach (var i in CollectDirectoriesAndFiles(directory, d))
            {
                directory.Items.Add(i);
            }

            yield return directory;
        }

        foreach (var f in Directory.GetFiles(path))
        {
            var fileType = ProjectFileClassificator.Classify(f);
            yield return new ProjectFile { Name = Path.GetFileName(f), FileType = fileType, Parent = parent };
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed && disposing)
        {
            _projectChangedSubscription.Dispose();
            _globals.PropertyChanged -= GlobalsOnPropertyChanged;
        }
        base.Dispose(disposing);
    }
}