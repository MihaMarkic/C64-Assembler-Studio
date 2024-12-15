using System.Diagnostics;
using System.Runtime.InteropServices;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class ProjectFileWatcher: DisposableObject, IProjectFileWatcher
{
    private readonly ILogger<ProjectFileWatcher> _logger;
    private readonly IOsDependent _osDependent;
    /// <summary>
    /// Watches for target directory in case it gets deleted or renamed.
    /// </summary>
    private readonly FileSystemWatcher _fileWatcher;
    private readonly FileSystemWatcher _directoryWatcher;
    private readonly ProjectRootDirectory _rootDirectory;

    public ProjectFileWatcher(ProjectRootDirectory rootDirectory, ILogger<ProjectFileWatcher> logger, IOsDependent osDependent)
    {
        _logger = logger;
        _osDependent = osDependent;
        bool isEnabled = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        if (!isEnabled)
        {
            Debug.WriteLine("Project file watcher disabled on MacOS");
        }

        _rootDirectory = rootDirectory;
        _fileWatcher = new FileSystemWatcher(_rootDirectory.AbsolutePath)
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
        _fileWatcher.EnableRaisingEvents = isEnabled;
        
        _directoryWatcher = new FileSystemWatcher(_rootDirectory.AbsolutePath)
        {
            NotifyFilter = NotifyFilters.Attributes
                           | NotifyFilters.DirectoryName
        };
        _directoryWatcher.Created += OnDirectoryCreated;
        _directoryWatcher.Deleted += OnDirectoryDeleted;
        _directoryWatcher.Renamed += OnDirectoryRenamed;
        _directoryWatcher.Error += OnError;

        _directoryWatcher.IncludeSubdirectories = true;
        _directoryWatcher.EnableRaisingEvents = isEnabled;
        _logger.LogInformation("Watching directory {Directory}", _rootDirectory);
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
        OnCreated(e, directory => new ProjectDirectory(_osDependent.FileStringComparison)
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
        var target = FindMatchingDirectory(_rootDirectory, Path.GetDirectoryName(e.Name).ValueOrThrow(), _logger,
            _osDependent);

        if (target.Items.Any(i => string.Equals(i.Name, e.Name, _osDependent.FileStringComparison)))
        {
            _logger.LogError("File {File} already exists", e.FullPath);
            return;
        }

        target.Items.Add(creation(target));
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        OnCreated(e, directory => new ProjectFile(_osDependent.FileStringComparison)
        {
            Name = Path.GetFileName(e.Name).ValueOrThrow(),
            FileType = FileType.Assembler,
            Parent = directory,
        });
    }

    internal static ProjectDirectory FindMatchingDirectory(ProjectRootDirectory rootDirectory,
        string directoryRelativePath, ILogger logger, IOsDependent osDependent)
    {
        var parts = directoryRelativePath.Split(Path.DirectorySeparatorChar);
        ProjectDirectory? current = null;
        if (parts.Length == 1 && string.IsNullOrWhiteSpace(parts[0]))
        {
            return rootDirectory;
        }

        current = rootDirectory.Items.OfType<ProjectDirectory>()
            .SingleOrDefault(d => string.Equals(d.Name, parts[0], osDependent.FileStringComparison));
        if (current is not null)
        {
            foreach (string part in parts.Skip(1))
            {
                current = current.GetSubdirectories()
                    .SingleOrDefault(d => string.Equals(d.Name, part, osDependent.FileStringComparison));
                if (current is null)
                {
                    break;
                }
            }
        }

        if (current is null)
        {
            logger.LogError("Couldn't match root of {AddedFile} to {ProjectRoot}", directoryRelativePath,
                rootDirectory.AbsolutePath);
            throw new Exception($"Couldn't match root of {directoryRelativePath} to {rootDirectory.AbsolutePath}");
        }

        return current;
    }


    private void OnDeleted(FileSystemEventArgs e)
    {
        var target = FindMatchingDirectory(_rootDirectory, Path.GetDirectoryName(e.Name).ValueOrThrow(), _logger,
            _osDependent);

        var fileName = Path.GetFileName(e.Name);
        var item = target.Items.SingleOrDefault(i =>
            string.Equals(i.Name, fileName, _osDependent.FileStringComparison));
        if (item is null)
        {
            _logger.LogError("Couldn't find {Item} to delete", e.FullPath);
            return;
        }

        target.Items.Remove(item);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        OnDeleted(e);
    }

    private void OnRenamed(RenamedEventArgs e)
    {
        _logger.LogDebug("Renamed file {OldName} to {NewName}", e.OldName, e.Name);
        var target = FindMatchingDirectory(_rootDirectory, Path.GetDirectoryName(e.OldName).ValueOrThrow(), _logger,
            _osDependent);

        var oldFileName = Path.GetFileName(e.OldName);
        var item = target.Items.SingleOrDefault(i =>
            string.Equals(i.Name, oldFileName, _osDependent.FileStringComparison));
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