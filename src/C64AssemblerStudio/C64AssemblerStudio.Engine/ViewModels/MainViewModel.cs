using System.ComponentModel;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using System.Runtime.CompilerServices;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Files;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using PropertyChanged;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Implementation;

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
    public RelayCommand CloseProjectCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ShowProjectSettingsCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommandAsync BuildCommand { get; }
    public RelayCommandAsync RunCommand { get; }
    public RelayCommandAsync PauseCommand { get; }
    public RelayCommandAsync StopCommand { get; }
    public RelayCommandAsync StepIntoCommand { get; }
    public RelayCommandAsync StepOverCommand { get; }
    public RelayCommandAsync ShowDisassemblyCommand { get; }
    public IProjectViewModel Project => _globals.Project;
    public ProjectExplorerViewModel ProjectExplorer { get; }
    public FilesViewModel Files { get; }
    public StartPageViewModel? StartPage { get; private set; }
    public ErrorMessagesViewModel ErrorMessages { get; }
    public BuildOutputViewModel BuildOutput { get; }
    public CompilerErrorsOutputViewModel CompilerErrors { get; }
    public ImmutableArray<IToolView> BottomTools { get; }
    public IToolView? SelectedBottomTool { get; set; }
    public StatusInfoViewModel StatusInfo { get; }
    // TODO implement
    public bool IsBusy => false;
    // TODO implement
    public bool IsDebugging => Vice.IsDebugging;
    public bool IsDebuggingPaused => Vice.IsPaused;
    public bool IsBuilding { get; private set; }
    public bool IsProjectOpen => _globals.Project is not EmptyProjectViewModel;
    /// <summary>
    /// Tracks whether user held shift when it performed an action.
    /// AvaloniaObject should set this property for each event when it needs to handle shift status.
    /// </summary>
    public bool IsShiftDown { get; set; }
    public string Caption => $"{Globals.AppName} - {(Project?.Configuration?.Caption ?? "no project")}";
    public bool IsShowingSettings => OverlayContent is SettingsViewModel;
    public bool IsShowingProject => OverlayContent is IProjectViewModel;
    public Action<ShowModalDialogMessageCore>? ShowModalDialog { get; set; }
    public Action? CloseApp { get; set; }
    public ViewModel? OverlayContent { get; private set; }
    public MainViewModel(ILogger<MainViewModel> logger, Globals globals, IDispatcher dispatcher, IServiceScope scope,
        ISettingsManager settingsManager, ProjectExplorerViewModel projectExplorer, FilesViewModel files,
        ErrorMessagesViewModel errorMessages, BuildOutputViewModel buildOutput, CompilerErrorsOutputViewModel compilerErrors,
        StatusInfoViewModel statusInfo, IVice vice)
    {
        _logger = logger;
        _globals = globals;
        _dispatcher = dispatcher;
        _scope = scope;
        Vice = vice;
        _settingsManager = settingsManager;
        _uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        _closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
        _showModalDialogMessageSubscription = dispatcher.Subscribe<ShowModalDialogMessageCore>(OnShowModalDialog);
        ProjectExplorer = projectExplorer;
        Files = files;
        ErrorMessages = errorMessages;
        BuildOutput = buildOutput;
        CompilerErrors = compilerErrors;
        StatusInfo = statusInfo;
        BottomTools = [ErrorMessages, CompilerErrors, BuildOutput];
        StartPage = _scope.ServiceProvider.CreateScopedContent<StartPageViewModel>();
        StartPage.LoadLastProjectRequest += StartPage_LoadLastProjectRequest;
        _commandsManager = new CommandsManager(this, _uiFactory);
        NewProjectCommand = _commandsManager.CreateRelayCommandAsync(CreateProjectAsync, () => !IsBusy && !IsDebugging);
        OpenProjectFromPathCommand = _commandsManager.CreateRelayCommand<string>(OpenProjectFromPath, _ => !IsBusy && !IsDebugging);
        OpenProjectCommand = _commandsManager.CreateRelayCommand(OpenProject, () => !IsBusy && !IsDebugging);
        ShowProjectSettingsCommand = _commandsManager.CreateRelayCommand(ShowProjectSettings, () => !IsShowingProject && IsProjectOpen);
        CloseProjectCommand = _commandsManager.CreateRelayCommand(CloseProject, () => IsProjectOpen && !IsDebugging);
        ShowSettingsCommand = _commandsManager.CreateRelayCommand(ShowSettings, () => !IsShowingSettings);
        ExitCommand = _commandsManager.CreateRelayCommand(() => CloseApp?.Invoke(), () => true);
        BuildCommand = _commandsManager.CreateRelayCommandAsync(BuildAsync, () => IsProjectOpen && !IsBuilding && !IsDebugging);
        RunCommand = _commandsManager.CreateRelayCommandAsync(
            StartDebuggingAsync, () => IsProjectOpen && (!IsDebugging || IsDebuggingPaused));
        if (!Directory.Exists(globals.Settings.VicePath))
        {
            SwitchOverlayContent<SettingsViewModel>();
        }
        Vice.PropertyChanged += ViceOnPropertyChanged;
        globals.PropertyChanged += Globals_PropertyChanged;
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
        }
    }

    async Task StartDebuggingAsync()
    {
        try
        {
            await Vice.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
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
            CompilerErrors.Clear();
            SelectedBottomTool = BuildOutput;
            var project = (KickAssProjectViewModel)Project;
            var settings =
                new KickAssemblerCompilerSettings(
                    @"D:\Git\Righthand\C64\C64-Assembler-Studio\binaries\KickAss\KickAss.jar");
            string directory = Project.Directory.ValueOrThrow();
            string file = "main.asm";
            await saveAllTask;
            var (errorCode, errors) =
                await project.Compiler.CompileAsync(file, directory, "build", settings, l => BuildOutput.AddLine(l));
            if (errorCode != 0)
            {
                StatusInfo.BuildingStatus = BuildStatus.Failure;
                var fileErorrs = errors.Select(e =>
                    new FileCompilerError(ProjectExplorer.GetProjectFileFromFullPath(e.Path), e))
                    .ToImmutableArray();
                if (!fileErorrs.IsEmpty)
                {
                    CompilerErrors.AddLines(fileErorrs);
                    SelectedBottomTool = CompilerErrors;
                }
            }
            else
            {
                await Project.LoadDebugDataAsync(ct);
                StatusInfo.BuildingStatus = BuildStatus.Success;
            }
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
                var project = _scope.ServiceProvider.CreateScopedContent<KickAssProjectViewModel>();
                project.Init(kickAssConfiguration, projectPath);
                _settingsManager.Save<Project>(kickAssConfiguration, projectPath, false);
                _globals.SetProject(project);
                ShowProjectSettings();
                return true;
            }
            catch (Exception ex)
            {
                _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Failed creating project", ex.Message));
            }
        }
        return false;
    }
    internal void ShowProjectSettings()
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
                await _dispatcher.DispatchAsync(new ErrorMessage(ErrorMessageLevel.Error, errorTitle, $"Project file {path} does not exist."), ct: ct);
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
                _globals.SetProject(projectViewModel);
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
    internal void CloseProject()
    {
        // DebuggerViewModel.CloseProject();
        _globals.ResetProject();
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
            case nameof(C64AssemblerStudio.Engine.Models.Projects.Project.Caption):
                OnPropertyChanged(nameof(Caption));
                break;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closeOverlaySubscription.Dispose();
            _showModalDialogMessageSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}
