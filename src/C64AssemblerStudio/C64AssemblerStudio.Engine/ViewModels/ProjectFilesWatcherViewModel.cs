using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ProjectFilesWatcherViewModel: ViewModel
{
    private readonly ILogger<ProjectFilesWatcherViewModel> _logger;
    private readonly IServiceFactory _serviceFactory;
    private readonly List<IProjectFileWatcher> _projectFileWatchers = new();
    private readonly Globals _globals;
    public bool IsRefreshing { get; private set; }
    public bool IsProjectChanging { get; private set; }
    public bool IsProjectOpen { get; private set; }
    public ObservableCollection<ProjectItem> Items => Root?.Items ?? [];
    public ProjectLibraries? Libraries { get; private set; }
    public ProjectRoot? Root { get; private set; }

    public ProjectFilesWatcherViewModel(ILogger<ProjectFilesWatcherViewModel> logger, IServiceFactory serviceFactory,
        Globals globals)
    {
        _logger = logger;
        _serviceFactory = serviceFactory;
        _globals = globals;
        _globals.PropertyChanged += GlobalsOnPropertyChanged;
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
                Start(project.Directory, project.Libraries);
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
                var ct = _refreshCts.Token;

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
                var projectRefreshTask = RefreshProjectStructureAsync(Root, ct);
                tasks.Add(projectRefreshTask);
                for (int i = 0; i < project.Libraries.Length; i++)
                {
                    var libraryPath = project.Libraries[i];
                    string absoluteLibraryPath = Path.GetFullPath(Path.Combine(directory, libraryPath));
                    var library = new ProjectLibrary
                    {
                        Name = Path.GetFileNameWithoutExtension(libraryPath),
                        Parent = null,
                        AbsoluteRootPath = absoluteLibraryPath,
                    };
                    Libraries.Items.Add(library);
                    tasks.Add(RefreshProjectStructureAsync(library, ct));
                }

                await Task.WhenAll(tasks);
            }
        }
        finally
        {
            IsRefreshing = false;
        }
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
                _logger.LogError("Project directory {Directory} does not exist", rootDirectory.AbsolutePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed refresh");
        }
    }
    
    void Start(string rootDirectory, ImmutableArray<string> libraries)
    {
        var fileWatcher = _serviceFactory.CreateProjectFileWatcher(Root.ValueOrThrow());
        _projectFileWatchers.Add(fileWatcher);
        for (int i = 0; i < libraries.Length; i++)
        {
            var libraryPath = libraries[i];
            var library = Libraries.Items[i];
            var absoluteLibraryDirectory = Path.Combine(rootDirectory, libraryPath);
            var libraryFileWatcher =  _serviceFactory.CreateProjectFileWatcher(library);
            _projectFileWatchers.Add(libraryFileWatcher);
        }
    }

    void Disengage()
    {
        foreach (var fileWatcher in _projectFileWatchers)
        {
            fileWatcher.Dispose();
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
            _globals.PropertyChanged -= GlobalsOnPropertyChanged;
        }
        base.Dispose(disposing);
    }
}