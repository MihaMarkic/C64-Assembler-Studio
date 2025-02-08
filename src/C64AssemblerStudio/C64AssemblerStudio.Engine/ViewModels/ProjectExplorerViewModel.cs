using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Extensions;
using C64AssemblerStudio.Core.Services.Abstract;
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
    private readonly IOsDependent _osDependent;
    private readonly IDirectoryService _directoryService;
    private readonly IFileService _fileService;
    public bool IsRefreshing => _projectFilesWatcher.IsRefreshing;
    public bool IsProjectOpen => _projectFilesWatcher.IsProjectOpen;
    public RelayCommandWithParameter<ProjectFile> OpenFileCommand { get; }
    public RelayCommand<ProjectDirectory> AddFileCommand { get; }
    public RelayCommand<ProjectDirectory> AddDirectoryCommand { get; }
    public RelayCommand AddLibraryCommand { get; }
    public RelayCommand<ProjectLibrary> AddRootLibraryFileCommand { get; }
    public RelayCommandAsync<ProjectLibrary> RemoveLibraryCommand { get; }
    public RelayCommand<ProjectLibrary> AddRootLibraryDirectoryCommand { get; }
    public RelayCommandWithParameterAsync<ProjectItem> RenameItemCommand { get; }
    public RelayCommandWithParameter<ProjectItem> DeleteItemCommand { get; }
    public RelayCommandWithParameter<ProjectItem> OpenInExplorerCommand { get; }
    public RelayCommandAsync RefreshCommand { get; }
    public ObservableCollection<ProjectItem> Items => _projectFilesWatcher.Items;

    public ProjectExplorerViewModel(ILogger<ProjectExplorerViewModel> logger, IDispatcher dispatcher,
        IServiceScopeFactory serviceScopeFactory, Globals globals, ProjectFilesWatcherViewModel projectFilesWatcher,
        IOsDependent osDependent, IDirectoryService directoryService, IFileService fileService)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _serviceScopeFactory = serviceScopeFactory;
        _globals = globals;
        _projectFilesWatcher = projectFilesWatcher;
        _osDependent = osDependent;
        _directoryService = directoryService;
        _fileService = fileService;
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
        RemoveLibraryCommand = new RelayCommandAsync<ProjectLibrary>(RemoveLibrary, l => l?.Exists ?? false);
        AddRootLibraryDirectoryCommand = new RelayCommand<ProjectLibrary>(AddRootLibraryDirectory);
        AddRootLibraryFileCommand = new RelayCommand<ProjectLibrary>(AddRootLibraryFile);
    }

    private void AddLibrary()
    {
        
    }

    private async Task RemoveLibrary(ProjectLibrary? library)
    {
        if (library is not null)
        {
            await _projectFilesWatcher.RemoveLibraryAsync(library, CancellationToken.None);
        }
    }

    private void OpenFile(ProjectFile file)
    {
        if (file.CanOpen)
        {
            var message = new OpenFileMessage(file);
            _dispatcher.Dispatch(message);
        }
        else
        {
            var path = file.AbsolutePath.ConvertsDirectorySeparators();
            Process.Start(_osDependent.FileAppOpenName, path);
        }
    }

    private void OpenInExplorer(ProjectItem item)
    {
        string path = item is ProjectDirectory ? item.AbsolutePath: item.AbsoluteDirectory;

        Process.Start(_osDependent.FileAppOpenName, path.ConvertsDirectorySeparators());
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
            case nameof(ProjectFilesWatcherViewModel.Items):
                OnPropertyChanged(nameof(Items));
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

    private void DeleteDirectory(ProjectDirectory directory)
    {
        try
        {
            _directoryService.Delete(directory.AbsolutePath, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deleting {Directory}", directory);
        }
    }

    private void DeleteFile(ProjectFile file)
    {
        try
        {
            _fileService.Delete(file.AbsolutePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deleting {File}", file);
        }
    }

    private void AddFile(ProjectDirectory? parent)
    {
        Debug.WriteLine($"Parent is null: {(parent is null)}");
        var correctParent = parent ?? _projectFilesWatcher.Root;
        _ = AddFileAsync(correctParent.ValueOrThrow());
    }
    private void AddRootLibraryFile(ProjectLibrary? library)
    {
        if (library is not null)
        {
            Debug.WriteLine($"Library name is {library?.Name} and absolute path is {library?.AbsolutePath}");
            _ = AddFileAsync(library!);
        }
    }

    private async Task AddFileAsync(ProjectDirectory directory)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddFileDialogViewModel>();
            detailViewModel.RootDirectory = directory.AbsolutePath;
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
        var correctParent = parent ?? _projectFilesWatcher.Root.ValueOrThrow();
        _ = AddDirectoryAsync(correctParent);
    }
    private void AddRootLibraryDirectory(ProjectLibrary? library)
    {
        if (library is not null)
        {
            _ = AddDirectoryAsync(library);
        }
    }

    private async Task AddDirectoryAsync(ProjectDirectory parent)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddDirectoryDialogViewModel>();
            detailViewModel.RootDirectory = parent.AbsolutePath;
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
            detailViewModel.RootDirectory = item.AbsoluteDirectory;
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
        if (directory is not null && fullPath.StartsWith(directory, _osDependent.FileStringComparison))
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
                .FirstOrDefault(i => part.Equals(i.Name, _osDependent.FileStringComparison));
            if (dir is null)
            {
                return null;
            }
            items = dir.Items;
        }

        string fileName = parts.Last();
        return items.OfType<ProjectFile>()
            .FirstOrDefault(i => fileName.Equals(i.Name, _osDependent.FileStringComparison));
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

