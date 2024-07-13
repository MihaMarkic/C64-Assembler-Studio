﻿using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using System.Runtime.CompilerServices;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using static System.Formats.Asn1.AsnWriter;

namespace C64AssemblerStudio.Engine.ViewModels;

public class MainViewModel : ViewModel
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly Globals _globals;
    private readonly IDispatcher _dispatcher;
    private readonly IServiceScope _scope;
    private readonly ISettingsManager _settingsManager;
    private readonly CommandsManager _commandsManager;
    private readonly TaskFactory _uiFactory;
    // subscriptions
    private readonly ISubscription _closeOverlaySubscription;
    private readonly ISubscription _showModalDialogMessageSubscription;
    public ObservableCollection<string> RecentProjects => _globals.Settings.RecentProjects;
    public Func<OpenFileDialogModel, CancellationToken, Task<string?>>? ShowCreateProjectFileDialogAsync { get; set; }
    public Func<OpenFileDialogModel, CancellationToken, Task<string?>>? ShowOpenProjectFileDialogAsync { get; set; }
    public RelayCommandAsync NewProjectCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand<string> OpenProjectFromPathCommand { get; }
    public RelayCommand CloseProjectCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ExitCommand { get; }
    public IProjectViewModel Project => _globals.Project;
    // TODO implement
    public bool IsBusy => false;
    // TODO implement
    public bool IsDebugging => false;
    public bool IsProjectOpen => _globals.Project is not EmptyProjectViewModel;
    /// <summary>
    /// Tracks whether user held shift when it performed an action.
    /// AvaloniaObject should set this property for each event when it needs to handle shift status.
    /// </summary>
    public bool IsShiftDown { get; set; }
    public string Caption => $"{Globals.AppName} - {(Project.Path ?? "no project")}";
    public bool IsShowingSettings => OverlayContent is SettingsViewModel;
    public bool IsShowingProject => OverlayContent is IProjectViewModel;
    public Action<ShowModalDialogMessageCore>? ShowModalDialog { get; set; }
    public Action? CloseApp { get; set; }
    public ViewModel? OverlayContent { get; private set; }
    public MainViewModel(ILogger<MainViewModel> logger, Globals globals, IDispatcher dispatcher, IServiceScope scope,
        ISettingsManager settingsManager)
    {
        _logger = logger;
        _globals = globals;
        _dispatcher = dispatcher;
        _scope = scope;
        _settingsManager = settingsManager;
        _uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        _closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
        _showModalDialogMessageSubscription = dispatcher.Subscribe<ShowModalDialogMessageCore>(OnShowModalDialog);
        _commandsManager = new CommandsManager(this, _uiFactory);
        NewProjectCommand = _commandsManager.CreateRelayCommandAsync(CreateProjectAsync, () => !IsBusy && !IsDebugging);
        OpenProjectFromPathCommand = _commandsManager.CreateRelayCommand<string>(OpenProjectFromPath, _ => !IsBusy && !IsDebugging);
        OpenProjectCommand = _commandsManager.CreateRelayCommand(OpenProject, () => !IsBusy && !IsDebugging);
        //globals.PropertyChanged += Globals_PropertyChanged;
        CloseProjectCommand = _commandsManager.CreateRelayCommand(CloseProject, () => IsProjectOpen && !IsDebugging);
        ShowSettingsCommand = _commandsManager.CreateRelayCommand(ShowSettings, () => !IsShowingSettings);
        ExitCommand = new RelayCommand(() => CloseApp?.Invoke());
        if (!Directory.Exists(globals.Settings.VicePath))
        {
            SwitchOverlayContent<SettingsViewModel>();
        }
    }
    public async Task CreateProjectAsync()
    {
        if (ShowCreateProjectFileDialogAsync is not null)
        {
            var model = new OpenFileDialogModel(
                _globals.Settings.LastAccessedDirectory,
                "C64 Assembler Studio project",
                "*.cas");
            string? projectPath = await ShowCreateProjectFileDialogAsync(model, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                bool success = CreateProject(projectPath);
                if (success)
                {
                    _globals.Settings.AddRecentProject(projectPath);
                }
            }
        }
    }
    internal bool CreateProject(string projectPath)
    {
        if (File.Exists(projectPath))
        {
            _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", $"Project file {projectPath} already exists."));
            return false;
        }
        else
        {
            try
            {
                string projectName = Path.GetFileNameWithoutExtension(projectPath);
                var kickAssConfiguration = new KickAssProject
                {
                    Caption = projectName,
                };
                var project = _scope.ServiceProvider.GetRequiredService<KickAssProjectViewModel>();
                project.Init(kickAssConfiguration, projectPath);
                _settingsManager.Save<Project>(kickAssConfiguration, projectPath, false);
                _globals.Project = project;
                ShowProject();
                return true;
            }
            catch (Exception ex)
            {
                _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", ex.Message));
            }
        }
        return false;
    }
    internal void ShowProject()
    {
        if (!IsShowingProject)
        {
            SwitchOverlayContent((ViewModel)_globals.Project);
        }
    }
    public async void OpenProject()
    {
        if (ShowOpenProjectFileDialogAsync is not null)
        {
            var model = new OpenFileDialogModel(
                _globals.Settings.LastAccessedDirectory,
                "C64 Assembler Studio project",
                "*.cas");
            string? projectPath = await ShowOpenProjectFileDialogAsync(model, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                _ = OpenProjectFromPathInternalAsync(projectPath);
            }
        }
    }
    internal async void OpenProjectFromPath(string? path)
    {
        // runs async because it manipulates most recent list
        await Task.Delay(1);
        CloseProject();
        await OpenProjectFromPathInternalAsync(path);
    }
    internal async Task<bool> OpenProjectFromPathInternalAsync(string? path, CancellationToken ct = default)
    {
        const string ErrorTitle = "Failed opening project";
        if (path is null)
        {
            return false;
        }
        //executionStatusViewModel.IsOpeningProject = true;
        try
        {
            if (!File.Exists(path))
            {
                _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, $"Project file {path} does not exist."));
                return false;
            }
            var projectConfiguration = _settingsManager.Load<Project>(path);
            if (projectConfiguration is null)
            {
                return false;
            }

            if (projectConfiguration is KickAssProject kickAssConfiguration)
            {
                var projectViewModel = _scope.ServiceProvider.GetRequiredService<KickAssProjectViewModel>();
                projectViewModel.Init(kickAssConfiguration, path);
            }
            else
            {
                throw new Exception("Not supported project");
            }
            _globals.Settings.AddRecentProject(path);
        }
        catch (Exception ex)
        {
            _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, ErrorTitle, ex.Message));
        }
        finally
        {
            // _executionStatusViewModel.IsOpeningProject = false;
        }
        return false;
    }
    internal void CloseProject()
    {
        // DebuggerViewModel.CloseProject();
        _globals.ResetProject();
    }
    void OnShowModalDialog(ShowModalDialogMessageCore message)
    {
        ShowModalDialog?.Invoke(message);
    }
    protected override void OnPropertyChanged([CallerMemberName] string name = default!)
    {
        base.OnPropertyChanged(name);
    }
    internal void ShowSettings()
    {
        if (!IsShowingSettings)
        {
            SwitchOverlayContent<SettingsViewModel>();
        }
    }

    private bool _shouldDisposeOverlay;
    internal void SwitchOverlayContent<T>(T overlayContent)
        where T: ViewModel
    {
        DisposeOverlay();
        OverlayContent = overlayContent;
        _shouldDisposeOverlay = false;
    }
    internal void SwitchOverlayContent<T>()
        where T : ScopedViewModel
    {
        DisposeOverlay();
        OverlayContent = _scope.ServiceProvider.CreateScopedContent<T>();
        _shouldDisposeOverlay = true;
    }

    void DisposeOverlay()
    {
        if (_shouldDisposeOverlay)
        {
            OverlayContent?.Dispose();
        }
    }
    internal void CloseOverlay(CloseOverlayMessage message)
    {
        if (OverlayContent is not null)
        {
            if (_shouldDisposeOverlay)
            {
                OverlayContent.Dispose();
            }
            OverlayContent = null;
        }
    }
}