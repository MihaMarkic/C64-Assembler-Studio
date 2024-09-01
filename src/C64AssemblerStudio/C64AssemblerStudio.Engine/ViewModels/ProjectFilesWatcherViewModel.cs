using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ProjectFilesWatcherViewModel: ViewModel
{
    private readonly ILogger<ProjectFilesWatcherViewModel> _logger;
    private FileSystemWatcher? _fileWatcher;
    private FileSystemWatcher? _directoryWatcher;
    private readonly Globals _globals;
    public bool IsRefreshing { get; private set; }
    public bool IsProjectChanging { get; private set; }
    [MemberNotNullWhen(true, nameof(_fileWatcher), nameof(_directoryWatcher))]
    public bool IsProjectOpen { get; private set; }
    public ObservableCollection<ProjectItem> Items { get; } = new();
    public ProjectFilesWatcherViewModel(ILogger<ProjectFilesWatcherViewModel> logger, Globals globals)
    {
        _logger = logger;
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
                Start(project.Directory);
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
            Items.Clear();
            if (_globals.Project is KickAssProjectViewModel project)
            {
                _refreshCts = new CancellationTokenSource();
                var ct = _refreshCts.Token;
                
                var directory = Path.GetDirectoryName(project.Path);
                try
                {
                    if (Directory.Exists(directory))
                    {
                        var items = await Task.Run(() =>
                        {
                            var items = new List<ProjectItem>();
                            foreach (var i in CollectDirectoriesAndFiles(parent: null, directory))
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    break;
                                }

                                items.Add(i);
                            }
                            return items;
                        }, ct);
                        ct.ThrowIfCancellationRequested();
                        foreach (var i in items)
                        {
                            Items.Add(i);
                        }
                    }
                    else
                    {
                        _logger.LogError("Project directory {Directory} does not exist", directory);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed refresh");
                }
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    
    void Start(string path)
    {
#if DEBUG
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Debug.WriteLine("Project file watcher disabled on MacOS");
            return;
        }
#endif
        _fileWatcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.Attributes
                           | NotifyFilters.FileName
                           | NotifyFilters.LastWrite
        };
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Created += OnFileCreated;
        _fileWatcher.Deleted += OnFileDeleted;
        _fileWatcher.Renamed += OnFileRenamed;
        _fileWatcher.Error += OnError;

        Debug.WriteLine("Watching files");
        _fileWatcher.IncludeSubdirectories = true;
        _fileWatcher.EnableRaisingEvents = true;
        
        _directoryWatcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.Attributes
                           | NotifyFilters.DirectoryName
        };
        _directoryWatcher.Created += OnDirectoryCreated;
        _directoryWatcher.Deleted += OnDirectoryDeleted;
        _directoryWatcher.Renamed += OnDirectoryRenamed;
        _directoryWatcher.Error += OnError;

        _directoryWatcher.IncludeSubdirectories = true;
        _directoryWatcher.EnableRaisingEvents = true;
    }
    IEnumerable<ProjectItem> CollectDirectoriesAndFiles(ProjectDirectory? parent, string path)
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
            string extension = Path.GetExtension(f);
            var fileType = Path.GetExtension(f) switch
            {
                ".cas" => FileType.Project,
                ".asm" => FileType.Assembler,
                _ => FileType.Other,
            };
            yield return new ProjectFile { Name = Path.GetFileName(f), FileType = fileType, Parent = parent };
        }
    }
    void Disengage()
    {
        if (_fileWatcher is not null)
        {
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Created -= OnFileCreated;
            _fileWatcher.Deleted -= OnFileDeleted;
            _fileWatcher.Renamed -= OnFileRenamed;
            _fileWatcher.Error -= OnError;
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Dispose();
            _fileWatcher = null;
        }

        if (_directoryWatcher is not null)
        {
            _directoryWatcher.Created -= OnDirectoryCreated;
            _directoryWatcher.Deleted -= OnDirectoryDeleted;
            _directoryWatcher.Renamed -= OnDirectoryRenamed;
            _directoryWatcher.Error -= OnError;
            _directoryWatcher.EnableRaisingEvents = false;
            _directoryWatcher.Dispose();
            _directoryWatcher = null;
        }
    }

    private void OnDirectoryCreated(object sender, FileSystemEventArgs e)
    {
        OnCreated(e, directory => new ProjectDirectory
        {
            Name = Path.GetFileName(e.Name).ValueOrThrow(), 
            Parent = directory
        });
    }
    private void OnDirectoryDeleted(object sender, FileSystemEventArgs e)
    {
        OnDeleted(e);
    }
    private void OnDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        OnRenamed(e);
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }
        Console.WriteLine($"Changed: {e.FullPath}");
    }

    private void OnCreated(FileSystemEventArgs e, Func<ProjectDirectory?, ProjectItem> creation)
    {
        _logger.LogDebug("Created file {File}", e.FullPath);
        var directory = FindMatchingDirectory(
            _globals.Project.Directory.ValueOrThrow(),
            Path.GetDirectoryName(e.Name).ValueOrThrow());
        ObservableCollection<ProjectItem> target = directory is null ? Items : directory.Items;

        if (target.Any(i => string.Equals(i.Name, e.Name, OsDependent.FileStringComparison)))
        {
            _logger.LogError("File {File} already exists", e.FullPath);
            return;
        }

        target.Add(creation(directory));
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        OnCreated(e, directory => new ProjectFile
        {
            Name = Path.GetFileName(e.Name).ValueOrThrow(), 
            FileType = FileType.Assembler, 
            Parent = directory
        });
    }

    internal ProjectDirectory? FindMatchingDirectory(string projectPath, string directoryRelativePath)
    {
        var parts = directoryRelativePath.Split(Path.DirectorySeparatorChar);
        ProjectDirectory? current = null;
        if (parts.Length == 1 && string.IsNullOrWhiteSpace(parts[0]))
        {
            return null;
        }

        current = Items.OfType<ProjectDirectory>().SingleOrDefault(d => string.Equals(d.Name, parts[0], OsDependent.FileStringComparison));
        if (current is not null)
        {
            foreach (string part in parts.Skip(1))
            {
                current = current.GetSubdirectories().SingleOrDefault(d => string.Equals(d.Name, part, OsDependent.FileStringComparison));
                if (current is null)
                {
                    break;
                }
            }
        }

        if (current is null)
        {
            _logger.LogError("Couldn't match root of {AddedFile} to {ProjectRoot}", directoryRelativePath, projectPath);
            throw new Exception($"Couldn't match root of {directoryRelativePath} to {projectPath}");
        }
        return current;
    }

    
    private void OnDeleted(FileSystemEventArgs e)
    {
        _logger.LogDebug("Created file {File}", e.FullPath);
        var directory = FindMatchingDirectory(
            _globals.Project.Directory.ValueOrThrow(),
            Path.GetDirectoryName(e.Name).ValueOrThrow());
        ObservableCollection<ProjectItem> target = directory is null ? Items : directory.Items;

        var fileName = Path.GetFileName(e.Name);
        var item = target.SingleOrDefault(i => string.Equals(i.Name, fileName, OsDependent.FileStringComparison)); 
        if (item is null)
        {
            _logger.LogError("Couldn't find {Item} to delete", e.FullPath);
            return;
        }

        target.Remove(item);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        OnDeleted(e);
    }

    private void OnRenamed(RenamedEventArgs e)
    {
        _logger.LogDebug("Renamed file {OldName} to {NewName}", e.OldName, e.Name);
        var directory = FindMatchingDirectory(
            _globals.Project.Directory.ValueOrThrow(),
            Path.GetDirectoryName(e.OldName).ValueOrThrow());
        ObservableCollection<ProjectItem> target = directory is null ? Items : directory.Items;

        var oldFileName = Path.GetFileName(e.OldName);
        var item = target.SingleOrDefault(i => string.Equals(i.Name, oldFileName, OsDependent.FileStringComparison)); 
        if (item is null)
        {
            _logger.LogError("Couldn't find {Item} to rename", e.FullPath);
            return;
        }

        item.Name = Path.GetFileName(e.Name).ValueOrThrow();
    }
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        OnRenamed(e);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "Watcher failure");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disengage();
        }
        base.Dispose(disposing);
    }
}