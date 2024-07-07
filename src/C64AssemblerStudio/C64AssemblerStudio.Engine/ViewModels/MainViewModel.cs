using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace C64AssemblerStudio.Engine.ViewModels;

public class MainViewModel : ViewModel
{
    readonly ILogger<MainViewModel> _logger;
    readonly Globals _globals;
    readonly IDispatcher _dispatcher;
    readonly IServiceScope _scope;
    readonly CommandsManager commandsManager;
    readonly TaskFactory uiFactory;
    // subscriptions
    readonly ISubscription closeOverlaySubscription;
    readonly ISubscription showModalDialogMessageSubscription;
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ExitCommand { get; }
    public ProjectViewModel Project => _globals.Project;
    /// <summary>
    /// Tracks whether user held shift when it performed an action.
    /// AvaloniaObject should set this property for each event when it needs to handle shift status.
    /// </summary>
    public bool IsShiftDown { get; set; }
    public string Caption => $"{Globals.AppName} - {(Project.Path ?? "no project")}";
    public bool IsShowingSettings => OverlayContent is SettingsViewModel;
    public Action<ShowModalDialogMessageCore>? ShowModalDialog { get; set; }
    public Action? CloseApp { get; set; }
    public ScopedViewModel? OverlayContent { get; private set; }
    public MainViewModel(ILogger<MainViewModel> logger, Globals globals, IDispatcher dispatcher, IServiceScope scope)
    {
        _logger = logger;
        _globals = globals;
        _dispatcher = dispatcher;
        _scope = scope;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        closeOverlaySubscription = dispatcher.Subscribe<CloseOverlayMessage>(CloseOverlay);
        showModalDialogMessageSubscription = dispatcher.Subscribe<ShowModalDialogMessageCore>(OnShowModalDialog);
        commandsManager = new CommandsManager(this, uiFactory);
        ShowSettingsCommand = commandsManager.CreateRelayCommand(ShowSettings, () => !IsShowingSettings);
        ExitCommand = new RelayCommand(() => CloseApp?.Invoke());
        if (!Directory.Exists(globals.Settings.VicePath))
        {
            SwitchOverlayContent<SettingsViewModel>();
        }
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
    internal void SwitchOverlayContent<T>()
        where T : ScopedViewModel
    {
        OverlayContent?.Dispose();
        OverlayContent = _scope.ServiceProvider.CreateScopedContent<T>();
    }
    internal void CloseOverlay(CloseOverlayMessage message)
    {
        if (OverlayContent is not null)
        {
            OverlayContent.Dispose();
            OverlayContent = null;
        }
    }
}
