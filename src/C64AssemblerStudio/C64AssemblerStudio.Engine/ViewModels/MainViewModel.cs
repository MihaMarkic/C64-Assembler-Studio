using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Files;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models;

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
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IParserManager _parserManager;
    private readonly IDirectoryService _directoryService;
    private readonly IFactory? _dockFactory;
    public IVice Vice { get; }

    // subscriptions
    private readonly ISubscription _closeOverlaySubscription;
    private readonly ISubscription _showModalDialogMessageSubscription;
    public ObservableCollection<string> RecentProjects => _globals.Settings.RecentProjects;
    public Func<OpenFileDialogModel, CancellationToken, Task<string?>>? ShowCreateProjectFileDialogAsync { get; set; }
    public Func<OpenFileDialogModel, CancellationToken, Task<string?>>? ShowOpenProjectFileDialogAsync { get; set; }
    public RelayCommandAsync NewProjectCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand<string> OpenProjectFromPathCommand { get; }
    public RelayCommandAsync CloseProjectCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ShowAboutCommand { get; }
    public RelayCommand ShowProjectSettingsCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommandAsync BuildCommand { get; }
    public RelayCommandAsync RunCommand { get; }
    public RelayCommandAsync PauseCommand { get; }
    public RelayCommandAsync StopCommand { get; }
    public RelayCommandAsync StepIntoCommand { get; }
    public RelayCommandAsync StepOverCommand { get; }
    public IProjectViewModel Project => _globals.Project;
    public ProjectExplorerViewModel ProjectExplorer { get; }
    public FilesViewModel Files { get; }
    public StartPageViewModel? StartPage { get; private set; }
    public ErrorMessagesViewModel ErrorMessages { get; }
    public BuildOutputViewModel BuildOutput { get; }
    public DebugOutputViewModel DebugOutput { get; }
    public ErrorsOutputViewModel Errors { get; }
    public ImmutableArray<IToolView> BottomTools { get; }
    public IToolView? SelectedBottomTool { get; set; }
    public StatusInfoViewModel StatusInfo { get; }
    public RegistersViewModel Registers { get; }
    public BreakpointsViewModel Breakpoints { get; }
    public MemoryViewerViewModel MemoryViewer { get; }
    
    public IRootDock Layout { get; }

    public CallStackViewModel CallStack { get; }

    // TODO implement
    public bool IsBusy => false;
    public bool IsDebuggingStarting { get; private set; }
    public bool IsDebugging => Vice.IsDebugging || IsDebuggingStarting;
    public bool IsDebuggingPaused => Vice.IsPaused;
    public bool IsBuilding { get; private set; }
    public bool IsProjectOpen => _globals.IsProjectOpen;

    /// <summary>
    /// Tracks whether user held shift when it performed an action.
    /// AvaloniaObject should set this property for each event when it needs to handle shift status.
    /// </summary>
    public bool IsShiftDown { get; set; }

    public string Caption => $"{Globals.AppName} - {(Project?.Configuration?.Caption ?? "no project")}";
    public bool IsShowingSettings => OverlayContent is SettingsViewModel;
    public bool IsShowingAbout => OverlayContent is AboutViewModel;
    public bool IsShowingProject => OverlayContent is IProjectViewModel;
    public Action<ShowModalDialogMessageCore>? ShowModalDialog { get; set; }
    public Action? CloseApp { get; set; }
    public ViewModel? OverlayContent { get; private set; }

    public MainViewModel(ILogger<MainViewModel> logger, Globals globals, IDispatcher dispatcher, IServiceScope scope,
        ISettingsManager settingsManager, ProjectExplorerViewModel projectExplorer, FilesViewModel files,
        ErrorMessagesViewModel errorMessages, BuildOutputViewModel buildOutput, DebugOutputViewModel debugOutput,
        ErrorsOutputViewModel errors, BreakpointsViewModel breakpoints,
        MemoryViewerViewModel memoryViewer,
        StatusInfoViewModel statusInfo, RegistersViewModel registers, IVice vice, IHostEnvironment hostEnvironment,
        CallStackViewModel callStack, IParserManager parserManager, IDirectoryService directoryService,
        IFactory dockFactory)
    {
        _logger = logger;
        _globals = globals;
        _dispatcher = dispatcher;
        _scope = scope;
        Vice = vice;
        _hostEnvironment = hostEnvironment;
        _settingsManager = settingsManager;
        _parserManager = parserManager;
        _uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        _closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
        _showModalDialogMessageSubscription = dispatcher.Subscribe<ShowModalDialogMessageCore>(OnShowModalDialog);
        _directoryService = directoryService;
        ProjectExplorer = projectExplorer;
        Files = files;
        ErrorMessages = errorMessages;
        BuildOutput = buildOutput;
        DebugOutput = debugOutput;
        Errors = errors;
        StatusInfo = statusInfo;
        Registers = registers;
        Breakpoints = breakpoints;
        MemoryViewer = memoryViewer;
        CallStack = callStack;
        BottomTools =
            [ErrorMessages, Errors, BuildOutput, DebugOutput, Registers, Breakpoints, MemoryViewer, CallStack];
        _dockFactory = dockFactory;
        Layout = _dockFactory.CreateLayout()!;
        _dockFactory.InitLayout(Layout);
        CreateStartPage();
        _commandsManager = new CommandsManager(this, _uiFactory);
        NewProjectCommand = _commandsManager.CreateRelayCommandAsync(CreateProjectAsync, () => !IsBusy && !IsDebugging);
        OpenProjectFromPathCommand =
            _commandsManager.CreateRelayCommand<string>(OpenProjectFromPath, _ => !IsBusy && !IsDebugging);
        OpenProjectCommand = _commandsManager.CreateRelayCommand(OpenProject, () => !IsBusy && !IsDebugging);
        ShowProjectSettingsCommand =
            _commandsManager.CreateRelayCommand(ShowProjectSettings, () => !IsShowingProject && IsProjectOpen);
        CloseProjectCommand =
            _commandsManager.CreateRelayCommandAsync(CloseProjectAsync, () => IsProjectOpen && !IsDebugging);
        ShowSettingsCommand = _commandsManager.CreateRelayCommand(ShowSettings, () => !IsShowingSettings);
        ShowAboutCommand = _commandsManager.CreateRelayCommand(ShowAbout, () => !IsShowingAbout);
        ExitCommand = _commandsManager.CreateRelayCommand(() => CloseApp?.Invoke(), () => true);
        BuildCommand =
            _commandsManager.CreateRelayCommandAsync(BuildAsync, () => IsProjectOpen && !IsBuilding && !IsDebugging);
        RunCommand = _commandsManager.CreateRelayCommandAsync(StartDebuggingAsync,
            () => IsProjectOpen && !(IsDebuggingStarting || IsDebugging && !IsDebuggingPaused));
        StopCommand = _commandsManager.CreateRelayCommandAsync(StopDebuggingAsync, () => IsDebuggingStarting || IsDebugging);
        PauseCommand =
            _commandsManager.CreateRelayCommandAsync(PauseDebuggingAsync, () => IsDebugging && !IsDebuggingPaused);
        StepIntoCommand =
            _commandsManager.CreateRelayCommandAsync(StepIntoAsync, () => IsDebugging && IsDebuggingPaused);
        StepOverCommand =
            _commandsManager.CreateRelayCommandAsync(StepOverAsync, () => IsDebugging && IsDebuggingPaused);
        if (!_directoryService.Exists(globals.Settings.VicePath))
        {
            SwitchOverlayContent<SettingsViewModel>();
        }

        Vice.PropertyChanged += ViceOnPropertyChanged;
        globals.PropertyChanged += Globals_PropertyChanged;
    }

    private void CreateStartPage()
    {
        StartPage = _scope.ServiceProvider.CreateScopedContent<StartPageViewModel>();
        var mostRecent = RecentProjects.FirstOrDefault();
        StartPage.HasRecentProjects = mostRecent is not null;
        StartPage.FullPath = mostRecent;
        StartPage.LoadLastProjectRequest += StartPage_LoadLastProjectRequest;
    }

    private void ViceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IVice.IsDebugging):
                OnPropertyChanged(nameof(IsDebugging));
                break;
            case nameof(IVice.IsPaused):
                OnPropertyChanged(nameof(IsDebuggingPaused));
                break;
            case nameof(IVice.IsConnected):
                if (!Vice.IsConnected && IsDebugging)
                {
                    DebugOutput.AddLine("Lost connection with VICE while debugging");
                    _ = StopDebuggingAsync();
                }
                break;
        }
    }

    internal async Task PauseDebuggingAsync()
    {
        DebugOutput.AddLine("Pausing");
        await Vice.PauseDebuggingAsync();
        DebugOutput.AddLine("Paused");
    }

    internal async Task StepIntoAsync()
    {
        DebugOutput.AddLine("Stepping into");
        await Vice.StepIntoAsync();
        DebugOutput.AddLine("Stepped into");
    }

    private async Task StepOverAsync()
    {
        DebugOutput.AddLine("Stepping over");
        await Vice.StepOverAsync();
        DebugOutput.AddLine("Stepped over");
    }

    private async Task StopDebuggingAsync()
    {
        if (IsDebuggingStarting)
        {
            DebugOutput.AddLine("Stopping");
            if (_debuggingCts is not null)
            {
                await _debuggingCts.CancelAsync();
                _debuggingCts.Dispose();
                _debuggingCts = null;
            }
        }

        DebugOutput.AddLine("Stopping");
        if (!Vice.IsConnected)
        {
            DebugOutput.AddLine("VICE not connected, won't clear its state");
        }
        
        // the two cleaning guys below should work even when VICE is not connected anymore
        await Breakpoints.DisarmAllBreakpointsAsync();
        await Vice.StopDebuggingAsync();
        DebugOutput.AddLine("Stopped");
    }

    private CancellationTokenSource? _debuggingCts;

    async Task StartDebuggingAsync()
    {
        try
        {
            if (IsDebuggingPaused)
            {
                DebugOutput.AddLine("Continuing");
                await Vice.ContinueAsync();
            }
            else
            {
                IsDebuggingStarting = true;
                _debuggingCts = new CancellationTokenSource();
                var ct = _debuggingCts.Token;
                DebugOutput.AddLine("Building");
                StatusInfo.DebuggingStatus = DebuggingStatus.Idle;
                var viceConnectTask = Vice.ConnectAsync(ct);
                await BuildAsync();
                if (StatusInfo.BuildingStatus == BuildStatus.Success)
                {
                    DebugOutput.AddLine("Build was successful");
                    StatusInfo.BuildingStatus = BuildStatus.Idle;
                    StatusInfo.DebuggingStatus = DebuggingStatus.WaitingForConnection;
                    DebugOutput.AddLine("Waiting for VICE connection");
                    try
                    {
                        await viceConnectTask;
                    }
                    catch (TimeoutException ex)
                    {
                        DebugOutput.AddLine("Timeout while waiting for connection");
                        _logger.LogError(ex, "Failed debugging");
                        StatusInfo.BuildingStatus = BuildStatus.Idle;
                        return;
                    }

                    DebugOutput.AddLine("Adding breakpoints from code");
                    await Breakpoints.AddBreakpointsFromCodeAsync(ct);
                    DebugOutput.AddLine("Arming breakpoints");
                    _logger.LogDebug("Arming breakpoints");
                    await Breakpoints.ArmBreakpointsAsync(ct);
                    DebugOutput.AddLine("Starting debugging");
                    _logger.LogDebug("Starting debugging");
                    await Vice.StartDebuggingAsync(ct);
                    _logger.LogDebug("Debugging started");
                }
            }
        }
        catch (Exception ex)
        {
            DebugOutput.AddLine($"Debugging error: {ex.Message}");
            _logger.LogError(ex, "Failed debugging");
            StatusInfo.DebuggingStatus = DebuggingStatus.Idle;
        }
        finally
        {
            IsDebuggingStarting = false;
        }
    }

    private void StartPage_LoadLastProjectRequest(object? sender, EventArgs e)
    {
        if (StartPage is not null)
        {
            OpenProjectFromPath(RecentProjects.First());
            StartPage.LoadLastProjectRequest -= StartPage_LoadLastProjectRequest;
            StartPage.Dispose();
            StartPage = null;
        }
    }

    private CancellationTokenSource? _buildCts;

    async Task BuildAsync()
    {
        await _buildCts.CancelNullableAsync();
        IsBuilding = true;
        StatusInfo.BuildingStatus = BuildStatus.Building;
        _buildCts = new CancellationTokenSource();
        var ct = _buildCts.Token;
        try
        {
            var saveAllTask = Files.SaveAllAsync();
            BuildOutput.Clear();
            Errors.Clear();
            SelectedBottomTool = BuildOutput;
            var project = (KickAssProjectViewModel)Project;
#if DEBUG
            string kickAssPath = Path.Combine(_hostEnvironment.ContentRootPath!, "..", "..", "..", "..", "..", "..",
                "binaries",
                "KickAss", "KickAss.jar");
#else
            string kickAssPath =
                Path.Combine(_hostEnvironment.ContentRootPath!, "KickAssembler", "KickAss.jar");
#endif
            var settings =
                new KickAssemblerCompilerSettings(null, 
                    [.._globals.Settings.Libraries.Values.Select(l => l.Path)],
                    project.SymbolsDefineSet);
            string directory = Project.Directory.ValueOrThrow();
            string file = "main.asm";
            await saveAllTask;
            var (errorCode, errors) =
                await project.Compiler.CompileAsync(file, directory, "build", settings, l => BuildOutput.AddLine(l));
            if (errorCode != 0)
            {
                StatusInfo.BuildingStatus = BuildStatus.Failure;
                var fileErrors = errors.Select(e =>
                        new FileCompilerError(ProjectExplorer.GetProjectFileFromFullPath(e.Path), e.Error))
                    .ToImmutableArray();
                if (!fileErrors.IsEmpty)
                {
                    Errors.AddCompilerErrors(fileErrors);
                    SelectedBottomTool = Errors;
                }
            }
            else
            {
                await Project.LoadDebugDataAsync(ct);
                StatusInfo.BuildingStatus = BuildStatus.Success;
            }
        }
        catch (Exception ex)
        {
            BuildOutput.AddLine(ex.Message);
            StatusInfo.BuildingStatus = BuildStatus.Failure;
            throw;
        }
        finally
        {
            IsBuilding = false;
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
                bool success = await CreateProject(projectPath, CancellationToken.None);
                if (success)
                {
                    _globals.Settings.AddRecentProject(projectPath);
                }
            }
        }
    }

    internal async Task<bool> CreateProject(string projectPath, CancellationToken ct)
    {
        try
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            var kickAssConfiguration = new KickAssProject
            {
                Caption = projectName,
            };
            var project = _scope.ServiceProvider.CreateScopedContent<KickAssProjectViewModel>();
            project.Init(kickAssConfiguration, projectPath);
            if (File.Exists(projectPath))
            {
                File.Delete(projectPath);
            }

            await _settingsManager.SaveAsync<Project>(kickAssConfiguration, projectPath, false);
            string mainAsmFile = Path.Combine(Path.GetDirectoryName(projectPath)!, "main.asm");
            if (!File.Exists(mainAsmFile))
            {
                var assembly = this.GetType().Assembly;
                await using (var s = assembly.GetManifestResourceStream(
                                 "C64AssemblerStudio.Engine.Resources.main.template")!)
                await using (var output = File.OpenWrite(mainAsmFile))
                {
                    await s.CopyToAsync(output, ct);
                }
            }

            await _globals.SetProjectAsync(project, ct);
            _ = _parserManager.RunInitialParseAsync(CancellationToken.None);
            ShowProjectSettings();
            return true;
        }
        catch (Exception ex)
        {
            await _dispatcher.DispatchAsync(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", ex.Message), ct: ct);
        }

        return false;
    }

    private void ShowProjectSettings()
    {
        if (!IsShowingProject)
        {
            SwitchOverlayContent((ViewModel)_globals.Project);
        }
    }

    private async void OpenProject()
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

    private async void OpenProjectFromPath(string? path)
    {
        // runs async because it manipulates most recent list
        await Task.Delay(1);
        if (await CloseProjectAsync())
        {
            await OpenProjectFromPathInternalAsync(path);
        }
    }

    private async Task<bool> OpenProjectFromPathInternalAsync(string? path, CancellationToken ct = default)
    {
        const string errorTitle = "Failed opening project";
        if (path is null)
        {
            return false;
        }

        //executionStatusViewModel.IsOpeningProject = true;
        try
        {
            if (!File.Exists(path))
            {
                await _dispatcher.DispatchAsync(
                    new ErrorMessage(ErrorMessageLevel.Error, errorTitle, $"Project file {path} does not exist."),
                    ct: ct);
                return false;
            }

            var projectConfiguration = await _settingsManager.LoadAsync<Project>(path, ct);
            if (projectConfiguration is null)
            {
                return false;
            }

            if (projectConfiguration is KickAssProject kickAssConfiguration)
            {
                var projectViewModel = _scope.ServiceProvider.CreateScopedContent<KickAssProjectViewModel>();
                projectViewModel.Init(kickAssConfiguration, path);
                await _globals.SetProjectAsync(projectViewModel, ct);
                _ = _parserManager.RunInitialParseAsync(CancellationToken.None);
            }
            else
            {
                throw new Exception("Not supported project");
            }

            _globals.Settings.AddRecentProject(path);
        }
        catch (Exception ex)
        {
            await _dispatcher.DispatchAsync(new ErrorMessage(ErrorMessageLevel.Error, errorTitle, ex.Message), ct: ct);
        }
        finally
        {
            // _executionStatusViewModel.IsOpeningProject = false;
        }

        return false;
    }

    private async Task<bool> CloseProjectAsync()
    {
        if (await CanCloseProject())
        {
            Files.RemoveProjectFiles();
            _globals.ResetProject();
            CreateStartPage();
            return true;
        }

        return false;
    }

    public async Task<bool> CanCloseProject()
    {
        if (IsDebugging)
        {
            DebugOutput.AddLine("Can't close project during debugging");
            return false;
        }
        
        if (Files.HasChanges)
        {
            try
            {
                bool allFilesClosed = await Files.CloseAllFilesAsync();
                if (!allFilesClosed)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed saving all files");
                return false;
            }
        }

        if (IsDebugging)
        {
            // TODO ask for debug stop
        }
        return true;
    }

    public async Task CloseAsync(CancellationToken ct = default)
    {
        try
        {
            await Vice.DisconnectAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed disconnecting");
        }
    }

    void OnShowModalDialog(ShowModalDialogMessageCore message)
    {
        ShowModalDialog?.Invoke(message);
    }

    internal void ShowSettings()
    {
        if (!IsShowingSettings)
        {
            SwitchOverlayContent<SettingsViewModel>();
        }
    }

    internal void ShowAbout()
    {
        if (!IsShowingAbout)
        {
            SwitchOverlayContent<AboutViewModel>();
        }
    }

    private Project? _oldProjectConfiguration;

    void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                if (_oldProjectConfiguration is not null)
                {
                    _oldProjectConfiguration.PropertyChanged -= OnProjectConfigurationPropertyChanged;
                }

                OnPropertiesChanged(nameof(IsProjectOpen), nameof(Project), nameof(Caption));
                if (_globals.Project is not EmptyProjectViewModel)
                {
                    _globals.Project.Configuration.ValueOrThrow().PropertyChanged +=
                        OnProjectConfigurationPropertyChanged;
                    _oldProjectConfiguration = _globals.Project.Configuration;
                }
                else
                {
                    _oldProjectConfiguration = null;
                }

                break;
        }
    }

    [SuppressPropertyChangedWarnings]
    private void OnProjectConfigurationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Models.Projects.Project.Caption):
                OnPropertyChanged(nameof(Caption));
                break;
        }
    }

    private bool _shouldDisposeOverlay;

    private void SwitchOverlayContent<T>(T overlayContent)
        where T : ViewModel
    {
        DisposeOverlay();
        OverlayContent = overlayContent;
        _shouldDisposeOverlay = false;
    }

    private void SwitchOverlayContent<T>()
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Layout.Close.CanExecute(null))
            {
                Layout.Close.Execute(null);
            }
            _closeOverlaySubscription.Dispose();
            _showModalDialogMessageSubscription.Dispose();
        }

        base.Dispose(disposing);
    }
}