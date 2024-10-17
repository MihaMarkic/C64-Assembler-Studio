using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class ProjectFileWatcher: DisposableObject, IProjectFileWatcher
{
    private readonly ILogger<ProjectFileWatcher> _logger;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly FileSystemWatcher _directoryWatcher;
    private readonly string _rootDirectory;
    private readonly ObservableCollection<ProjectItem> _items;

    public ProjectFileWatcher(string rootDirectory, ObservableCollection<ProjectItem> items,
        ILogger<ProjectFileWatcher> logger)
    {
        _logger = logger;
        _items = items;
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        // {
        //     Debug.WriteLine("Project file watcher disabled on MacOS");
        //     return;
        // }
        _rootDirectory = rootDirectory;
        _fileWatcher = new FileSystemWatcher(_rootDirectory)
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

        _fileWatcher.IncludeSubdirectories = true;
        _fileWatcher.EnableRaisingEvents = true;
        
        _directoryWatcher = new FileSystemWatcher(_rootDirectory)
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
        _logger.LogInformation("Watching directory {Directory}", _rootDirectory);
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
            var fileType = ProjectFileClassificator.Classify(f);
            yield return new ProjectFile { Name = Path.GetFileName(f), FileType = fileType, Parent = parent };
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed && disposing)
        {
            Disengage();
        }
        base.Dispose(disposing);
    }

    void Disengage()
    {
        _fileWatcher.Changed -= OnFileChanged;
        _fileWatcher.Created -= OnFileCreated;
        _fileWatcher.Deleted -= OnFileDeleted;
        _fileWatcher.Renamed -= OnFileRenamed;
        _fileWatcher.Error -= OnError;
        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Dispose();

        _directoryWatcher.Created -= OnDirectoryCreated;
        _directoryWatcher.Deleted -= OnDirectoryDeleted;
        _directoryWatcher.Renamed -= OnDirectoryRenamed;
        _directoryWatcher.Error -= OnError;
        _directoryWatcher.EnableRaisingEvents = false;
        _directoryWatcher.Dispose();
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
        var directory = FindMatchingDirectory(_rootDirectory,
            Path.GetDirectoryName(e.Name).ValueOrThrow());
        ObservableCollection<ProjectItem> target = directory is null ? _items : directory.Items;

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

        current = _items.OfType<ProjectDirectory>().SingleOrDefault(d => string.Equals(d.Name, parts[0], OsDependent.FileStringComparison));
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
        var directory = FindMatchingDirectory(_rootDirectory,
            Path.GetDirectoryName(e.Name).ValueOrThrow());
        ObservableCollection<ProjectItem> target = directory is null ? _items : directory.Items;

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
        var directory = FindMatchingDirectory(_rootDirectory,
            Path.GetDirectoryName(e.OldName).ValueOrThrow());
        ObservableCollection<ProjectItem> target = directory is null ? _items : directory.Items;

        var oldFileName = Path.GetFileName(e.OldName);
        var item = target.SingleOrDefault(i => string.Equals(i.Name, oldFileName, OsDependent.FileStringComparison)); 
        if (item is null)
        {
            _logger.LogError("Couldn't find {Item} to rename", e.FullPath);
            return;
        }

        item.Name = Path.GetFileName(e.Name).ValueOrThrow();
        if (item is ProjectFile file)
        {
            file.FileType = ProjectFileClassificator.Classify(item.Name);
        }
    }
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        OnRenamed(e);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "Watcher failure");
    }
}