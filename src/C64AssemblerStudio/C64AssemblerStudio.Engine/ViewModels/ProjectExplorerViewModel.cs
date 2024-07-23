using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

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
    }

    private void OpenFile(ProjectFile file)
    {
        var message = new OpenFileMessage(file);
        _dispatcher.Dispatch(message);
    }
    private void OpenInExplorer(ProjectItem item)
    {
        string path = Path.Combine(_globals.Project.Directory.ValueOrThrow(), item.GetRelativeDirectory());
        Process.Start("explorer.exe", path);
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

    private void DeleteDirectory(ProjectDirectory directory)
    {
        string path = Path.Combine(_globals.Project.Directory.ValueOrThrow(), directory.GetRelativeDirectory(), directory.Name);
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
        string path = Path.Combine(_globals.Project.Directory.ValueOrThrow(), file.GetRelativeDirectory(), file.Name);
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deleting {File}", file);
        }
    }

    internal void AddFile(ProjectDirectory? parent)
    {
        Debug.WriteLine($"Parent is null: {(parent is null)}");
        _ = AddFileAsync(parent);
    }

    string GetRootDirectory(ProjectDirectory? parent)
    {
        string projectRoot = Path.GetDirectoryName(_globals.Project.Path.ValueOrThrow()).ValueOrThrow();
        if (parent is not null)
        {
            return Path.Combine(projectRoot, parent.GetRelativeDirectory());
        }
        return projectRoot;
    }
    internal async Task AddFileAsync(ProjectDirectory? parent)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddFileDialogViewModel>();
            detailViewModel.RootDirectory = GetRootDirectory(parent);
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
    internal void AddDirectory(ProjectDirectory? parent)
    {
        _ = AddDirectoryAsync(parent);
    }
    internal async Task AddDirectoryAsync(ProjectDirectory? parent)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddDirectoryDialogViewModel>();
            detailViewModel.RootDirectory = GetRootDirectory(parent);
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
    internal async Task RenameItemAsync(ProjectItem item)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.GetRequiredService<RenameItemDialogViewModel>();
            detailViewModel.RootDirectory = GetRootDirectory(item.Parent);
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
        if (directory is not null && fullPath.StartsWith(directory, OSDependent.FileStringComparison))
        {
            var relativePath = fullPath[(directory.Length)..].TrimStart(System.IO.Path.DirectorySeparatorChar);
            return FindProjectFile(relativePath);
        }

        return null;
    }

    /// <summary>
    /// Finds <see cref="ProjectFile"/> based on relative path.
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public ProjectFile? FindProjectFile(string relativePath)
    {
        var parts = relativePath.Split(System.IO.Path.DirectorySeparatorChar);
        ObservableCollection<ProjectItem> items = Items;
        foreach (string part in parts[0..^1])
        {
            var dir = items.OfType<ProjectDirectory>()
                .FirstOrDefault(i => part.Equals(i.Name, OSDependent.FileStringComparison));
            if (dir is null)
            {
                return null;
            }
            items = dir.Items;
        }

        string fileName = parts.Last();
        return items.OfType<ProjectFile>()
            .FirstOrDefault(i => fileName.Equals(i.Name, OSDependent.FileStringComparison));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            //_globals.PropertyChanged -= GlobalsOnPropertyChanged;
        }

        base.Dispose(disposing);
    }
}

