using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Extensions;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ProjectExplorerViewModel : ViewModel
{
    private readonly ILogger<ProjectExplorerViewModel> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Globals _globals;
    private readonly ProjectFilesWatcherViewModel _projectFilesWatcher;
    public bool IsRefreshing => _projectFilesWatcher.IsRefreshing;
    public bool IsProjectOpen => _projectFilesWatcher.IsProjectOpen;
    public RelayCommandWithParameter<ProjectFile> OpenFileCommand { get; }
    public RelayCommand<ProjectDirectory> AddFileCommand { get; }
    public RelayCommand<ProjectDirectory> AddDirectoryCommand { get; }
    public RelayCommand AddLibraryCommand { get; }
    public RelayCommand<ProjectLibrary> AddRootLibraryFileCommand { get; }
    public RelayCommand<ProjectLibrary> RemoveLibraryCommand { get; }
    public RelayCommandWithParameterAsync<ProjectItem> RenameItemCommand { get; }
    public RelayCommandWithParameter<ProjectItem> DeleteItemCommand { get; }
    public RelayCommandWithParameter<ProjectItem> OpenInExplorerCommand { get; }
    public RelayCommandAsync RefreshCommand { get; }
    public ObservableCollection<ProjectItem> Items => _projectFilesWatcher.Items;

    public ProjectExplorerViewModel(ILogger<ProjectExplorerViewModel> logger, IDispatcher dispatcher,
        IServiceScopeFactory serviceScopeFactory, Globals globals, ProjectFilesWatcherViewModel projectFilesWatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _serviceScopeFactory = serviceScopeFactory;
        _globals = globals;
        _projectFilesWatcher = projectFilesWatcher;
        _projectFilesWatcher.PropertyChanged += ProjectFilesWatcherOnPropertyChanged;
        OpenFileCommand = new RelayCommandWithParameter<ProjectFile>(OpenFile);
        AddFileCommand = new RelayCommand<ProjectDirectory>(AddFile);
        AddDirectoryCommand = new RelayCommand<ProjectDirectory>(AddDirectory);
        RenameItemCommand = new RelayCommandWithParameterAsync<ProjectItem>(RenameItemAsync);
        DeleteItemCommand = new RelayCommandWithParameter<ProjectItem>(DeleteItem);
        RefreshCommand = new RelayCommandAsync(_projectFilesWatcher.RefreshAsync);
        OpenInExplorerCommand = new RelayCommandWithParameter<ProjectItem>(OpenInExplorer);
        // disabled for now
        AddLibraryCommand = new RelayCommand(AddLibrary, () => false);
        RemoveLibraryCommand = new RelayCommand<ProjectLibrary>(RemoveLibrary, l => l is not null && false);
        AddRootLibraryFileCommand = new RelayCommand<ProjectLibrary>(AddRootLibraryFile);
    }

    private void AddLibrary()
    {
        
    }

    private void RemoveLibrary(ProjectLibrary? library)
    {
        
    }
    private void OpenFile(ProjectFile file)
    {
        var message = new OpenFileMessage(file);
        _dispatcher.Dispatch(message);
    }

    private void OpenInExplorer(ProjectItem item)
    {
        string path;
        if (item is ProjectLibrary library)
        {
            path = library.AbsolutePath;
        }
        else if (item is ProjectDirectory directory)
        {
            path = GetAbsoluteDirectoryPath(directory);
        }
        else if (item is ProjectFile)
        {
            path = GetAbsoluteDirectoryForItem(item);
        }
        else
        {
            throw new ArgumentException($"Unsupported ProjectItem type {item.GetType().Name}", nameof(item));
        }

        Process.Start(OsDependent.FileAppOpenName, path.ConvertsDirectorySeparators());
    }

    private void ProjectFilesWatcherOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ProjectFilesWatcherViewModel.IsProjectOpen):
                OnPropertyChanged(nameof(IsProjectOpen));
                break;
            case nameof(ProjectFilesWatcherViewModel.IsRefreshing):
                OnPropertyChanged(nameof(IsRefreshing));
                break;
        }
    }
    void DeleteItem(ProjectItem item)
    {
        switch (item)
        {
            case ProjectFile file:
                DeleteFile(file);
                break;
            case ProjectDirectory directory:
                DeleteDirectory(directory);
                break;
            default:
                _logger.LogError("Unknown project item of type {Type}", item.GetType().Name);
                break;
        }
    }

    private string GetAbsoluteDirectoryPath(ProjectDirectory directory)
    {
        var relativeDirectoryPath = directory.GetRelativeDirectory();
        var rootParent = directory.GetRootDirectory();
        var rootDirectory = GetRootDirectory(directory);
        string result;
        if (rootParent is ProjectLibrary library)
        {
            result = Path.Combine(rootDirectory, relativeDirectoryPath, directory.Name);
        }
        else
        {
            result = Path.Combine(rootDirectory, directory.GetRelativeDirectory(), directory.Name);
        }

        return result;
    }

    private string GetAbsoluteDirectoryForItem(ProjectItem item)
    {
        var rootDirectory = GetRootDirectory(item);
        return Path.Combine(rootDirectory, item.GetRelativeDirectory());
    }

    private string GetRootDirectory(ProjectItem item)
    {
        var rootParent = item.GetRootDirectory();
        string result;
        if (rootParent is ProjectLibrary library)
        {
            return library.AbsolutePath;
        }
        else
        {
            return _globals.Project.Directory.ValueOrThrow();
        }
    }

    private void DeleteDirectory(ProjectDirectory directory)
    {
        string path = GetAbsoluteDirectoryPath(directory);
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deleting {Directory}", directory);
        }
    }

    private void DeleteFile(ProjectFile file)
    {
        string path = Path.Combine(GetAbsoluteDirectoryForItem(file), file.Name);
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deleting {File}", file);
        }
    }

    private void AddFile(ProjectDirectory? parent)
    {
        Debug.WriteLine($"Parent is null: {(parent is null)}");
        _ = AddFileAsync(parent);
    }
    private void AddRootLibraryFile(ProjectLibrary? library)
    {
        Debug.WriteLine($"Library name is {library?.Name} and absolute path is {library?.AbsolutePath}");
        _ = AddFileAsync(library);
    }

    private async Task AddFileAsync(ProjectDirectory? parent)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddFileDialogViewModel>();
            detailViewModel.RootDirectory = GetAbsoluteDirectoryPath(parent.ValueOrThrow());
            var message =
                new ShowModalDialogMessage<AddFileDialogViewModel, SimpleDialogResult>(
                    "Add new file",
                    DialogButton.OK | DialogButton.Cancel,
                    detailViewModel)
                {
                    MinSize = new Size(300, 200),
                    DesiredSize = new Size(500, 300),
                };
            _dispatcher.DispatchShowModalDialog(message);
            await message.Result;
        }
    }

    private void AddDirectory(ProjectDirectory? parent)
    {
        _ = AddDirectoryAsync(parent);
    }

    private async Task AddDirectoryAsync(ProjectDirectory? parent)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddDirectoryDialogViewModel>();
            detailViewModel.RootDirectory = GetAbsoluteDirectoryPath(parent.ValueOrThrow());
            var message =
                new ShowModalDialogMessage<AddDirectoryDialogViewModel, SimpleDialogResult>(
                    "Add new directory",
                    DialogButton.OK | DialogButton.Cancel,
                    detailViewModel)
                {
                    MinSize = new Size(300, 180),
                    DesiredSize = new Size(500, 180),
                };
            _dispatcher.DispatchShowModalDialog(message);
            await message.Result;
        }
    }

    private async Task RenameItemAsync(ProjectItem item)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<RenameItemDialogViewModel>();
            detailViewModel.RootDirectory = GetAbsoluteDirectoryForItem(item);
            detailViewModel.Item = item;
            string itemType = item is ProjectDirectory ? "directory" : "file";
            var message =
                new ShowModalDialogMessage<RenameItemDialogViewModel, SimpleDialogResult>(
                    $"Rename {itemType}",
                    DialogButton.OK | DialogButton.Cancel,
                    detailViewModel)
                {
                    MinSize = new Size(300, 120),
                    DesiredSize = new Size(500, 120),
                };
            _dispatcher.DispatchShowModalDialog(message);
            await message.Result;
        }
    }
    /// <summary>
    /// Finds <see cref="ProjectFile"/> based on full path.
    /// </summary>
    /// <param name="fullPath">Full path of file to find.</param>
    /// <returns></returns>
    public ProjectFile? GetProjectFileFromFullPath(string fullPath)
    {
        var directory = _globals.Project.Directory;
        if (directory is not null && fullPath.StartsWith(directory, OsDependent.FileStringComparison))
        {
            var relativePath = fullPath[(directory.Length)..].TrimStart(Path.DirectorySeparatorChar);
            return FindProjectFile(relativePath);
        }

        return null;
    }

    /// <summary>
    /// Finds <see cref="ProjectFile"/> based on relative path.
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    private ProjectFile? FindProjectFile(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar);
        ObservableCollection<ProjectItem> items = Items;
        foreach (string part in parts[0..^1])
        {
            var dir = items.OfType<ProjectDirectory>()
                .FirstOrDefault(i => part.Equals(i.Name, OsDependent.FileStringComparison));
            if (dir is null)
            {
                return null;
            }
            items = dir.Items;
        }

        string fileName = parts.Last();
        return items.OfType<ProjectFile>()
            .FirstOrDefault(i => fileName.Equals(i.Name, OsDependent.FileStringComparison));
    }
    
    public (ProjectFile File, FileLocation Location)? GetExecutionLocation(ushort address)
    {
        var debugData = ((KickAssProjectViewModel)_globals.Project).DbgData;
        // don't care until there is no debug data
        if (debugData is null)
        {
            return null;
        }
        var blockItems = debugData.Segments.SelectMany(s => s.Blocks).SelectMany(b => b.Items);
        var fileLocation = blockItems.SingleOrDefault(bi => bi.Start <= address && bi.End >= address)?.FileLocation;
        if (fileLocation is not null)
        {
            var source = debugData.Sources.Where(s => s.Origin == SourceOrigin.User)
                .SingleOrDefault(s => s.Index == fileLocation.SourceIndex);
            if (source is not null)
            {
                string? relativePath = source.GetRelativePath(_globals.Project.Directory.ValueOrThrow());
                if (relativePath is not null)
                {
                    var file = FindProjectFile(relativePath);
                    if (file is not null)
                    {
                        return (file, fileLocation);
                    }
                    else
                    {
                        _logger.LogWarning("Couldn't get {File} from project explorer", source.FullPath);
                    }
                }
                else
                {
                    _logger.LogWarning("Couldn't get relative path for {File}", source.FullPath);
                }
            }
            else
            {
                _logger.LogDebug("No line at address {Address:X4} within {SourceIndex} when execution paused", address,
                    fileLocation.SourceIndex);
            }
        }
        else
        {
            _logger.LogDebug("No source at address {Address:X4} when execution paused", address);
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _projectFilesWatcher.PropertyChanged -= ProjectFilesWatcherOnPropertyChanged;
        }

        base.Dispose(disposing);
    }
}

