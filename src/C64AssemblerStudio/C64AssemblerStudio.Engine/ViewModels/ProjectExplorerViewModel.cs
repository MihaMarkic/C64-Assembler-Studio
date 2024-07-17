using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
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
    public RelayCommand<ProjectDirectory> AddFileCommand { get; }
    public RelayCommand<ProjectDirectory> AddDirectoryCommand { get; }
    public RelayCommandAsync<ProjectItem> RenameItemCommand { get; }
    public RelayCommand<ProjectItem> DeleteItemCommand { get; }
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
        AddFileCommand = new RelayCommand<ProjectDirectory>(AddFile);
        AddDirectoryCommand = new RelayCommand<ProjectDirectory>(AddDirectory);
        RenameItemCommand = new RelayCommandAsync<ProjectItem>(RenameItemAsync, p => p is not null);
        DeleteItemCommand = new RelayCommand<ProjectItem>(DeleteItem, p => p is not null);
        RefreshCommand = new RelayCommandAsync(_projectFilesWatcher.RefreshAsync);
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
    void DeleteItem(ProjectItem? item)
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
                _logger.LogError("Unknown project item of type {Type}", item?.GetType().Name ?? "null");
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
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddFileViewModel>();
            detailViewModel.RootDirectory = GetRootDirectory(parent);
            var message =
                new ShowModalDialogMessage<AddFileViewModel, SimpleDialogResult>(
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
            var detailViewModel = scope.ServiceProvider.GetRequiredService<AddDirectoryViewModel>();
            detailViewModel.RootDirectory = GetRootDirectory(parent);
            var message =
                new ShowModalDialogMessage<AddDirectoryViewModel, SimpleDialogResult>(
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
            var detailViewModel = scope.ServiceProvider.GetRequiredService<RenameItemViewModel>();
            detailViewModel.RootDirectory = GetRootDirectory(item.Parent);
            detailViewModel.Item = item;
            string itemType = item is ProjectDirectory ? "directory" : "file";
            var message =
                new ShowModalDialogMessage<RenameItemViewModel, SimpleDialogResult>(
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
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            //_globals.PropertyChanged -= GlobalsOnPropertyChanged;
        }

        base.Dispose(disposing);
    }
}

