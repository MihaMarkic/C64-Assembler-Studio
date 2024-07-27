using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.ViewModels.Breakpoints;

public record AddressRange(ushort StartAddress, ushort Length)
{
    public static AddressRange FromRange(ushort startAddress, ushort endAddress)
        => new AddressRange(startAddress, (ushort)(endAddress - startAddress + 1));

    public ushort EndAddress => (ushort)(StartAddress + Length - 1);
    public bool IsAddressInRange(ushort address) => address >= StartAddress && address <= EndAddress;
}

public class BreakpointsViewModel : NotifiableObject, IToolView
{
    public enum BreakPointContextColumn
    {
        Binding,
        Other
    }

    public record BreakPointContext(BreakpointViewModel ViewModel, BreakPointContextColumn Column);

    public string Header => "Breakpoints";

    private readonly ILogger<BreakpointsViewModel> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly Globals _globals;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IVice _vice;
    private readonly ISettingsManager _settingsManager;
    private readonly CommandsManager _commandsManager;

    public ObservableCollection<BreakpointViewModel> Breakpoints { get; }
    /// <summary>
    /// Maps breakpoints by checkpoint number
    /// </summary>
    private readonly Dictionary<uint, BreakpointViewModel> _breakpointsMap = new ();
    private readonly Dictionary<int, List<BreakpointViewModel>> _breakpointsLinesMap = new();

    // readonly Dictionary<PdbLine, List<BreakpointViewModel>> breakpointsLinesMap;
    // readonly Dictionary<uint, BreakpointViewModel> breakpointsMap;
    readonly TaskFactory _uiFactory;
    public RelayCommandWithParameterAsync<BreakpointViewModel> ToggleBreakpointEnabledCommand { get; }
    public RelayCommandWithParameterAsync<BreakpointViewModel> ShowBreakpointPropertiesCommand { get; }
    public RelayCommandWithParameterAsync<BreakPointContext> BreakPointContextCommand { get; }
    public RelayCommandWithParameterAsync<BreakpointViewModel> RemoveBreakpointCommand { get; }
    public RelayCommandAsync RemoveAllBreakpointsCommand { get; }
    public RelayCommandAsync CreateBreakpointCommand { get; }
    public bool IsWorking { get; private set; }
    public bool IsProjectOpen => _globals.IsProjectOpen;

    /// <summary>
    /// When true, it shouldn't update breakpoints settings
    /// </summary>
    bool _suppressLocalPersistence;

    public BreakpointsViewModel(ILogger<BreakpointsViewModel> logger, IVice vice, IDispatcher dispatcher,
        Globals globals,
        IServiceScopeFactory serviceScopeFactory, ISettingsManager settingsManager)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _globals = globals;
        _vice = vice;
        _serviceScopeFactory = serviceScopeFactory;
        _settingsManager = settingsManager;
        _uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        _commandsManager = new CommandsManager(this, _uiFactory);
        Breakpoints = new ObservableCollection<BreakpointViewModel>();
        Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        // breakpointsLinesMap = new Dictionary<PdbLine, List<BreakpointViewModel>>();
        // breakpointsMap = new Dictionary<uint, BreakpointViewModel>();
        ToggleBreakpointEnabledCommand =
            _commandsManager.CreateRelayCommandWithParameterAsync<BreakpointViewModel>(ToggleBreakpointEnabledAsync);
        ShowBreakpointPropertiesCommand =
            _commandsManager.CreateRelayCommandWithParameterAsync<BreakpointViewModel>(ShowBreakpointPropertiesAsync);
        BreakPointContextCommand = _commandsManager.CreateRelayCommandWithParameterAsync<BreakPointContext>(BreakPointContextAsync);
        RemoveBreakpointCommand = _commandsManager.CreateRelayCommandWithParameterAsync<BreakpointViewModel>(RemoveBreakpointAsync);
        // TODO disable breakpoints manipulation when vice is not connected
        RemoveAllBreakpointsCommand = _commandsManager.CreateRelayCommandAsync(RemoveAllBreakpointsAsync, () => IsProjectOpen);
        CreateBreakpointCommand = _commandsManager.CreateRelayCommandAsync(CreateBreakpoint, () => IsProjectOpen);
        _vice.CheckpointInfoUpdated += ViceOnCheckpointInfoUpdated;
        globals.PropertyChanged += Globals_PropertyChanged;
    }

    private void ViceOnCheckpointInfoUpdated(object? sender, CheckpointInfoEventArgs e)
    {
        if (e.Response is not null)
        {
            _uiFactory.StartNew(r => { UpdateBreakpointDataFromVice((CheckpointInfoResponse)r!); }, e.Response);
        }
    }

    async void Breakpoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        await SaveLocalSettingsAsync();
    }

    public ImmutableArray<BreakpointViewModel> GetBreakpointsAssociatedWithLine(int lineNumber)
    {
        if (_breakpointsLinesMap.TryGetValue(lineNumber, out var breakpoints))
        {
            return [..breakpoints];
        }
        return ImmutableArray<BreakpointViewModel>.Empty;
    }

    void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                _ = RemoveAllBreakpointsAsync(false);
                OnPropertiesChanged(nameof(IsProjectOpen));
                break;
            // should happen only on start debug 
            //case nameof(Globals.ProjectDebugSymbols):
            //    if (!executionStatusViewModel.IsOpeningProject)
            //    {
            //        _ = ApplyOriginalBreakpointsOnNewPdbAsync(CancellationToken.None);
            //    }
            //    break;
        }
    }

    internal async Task CreateBreakpoint()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.CreateScopedBreakpointDetailViewModel(
                new BreakpointViewModel 
                { 
                    IsEnabled = true, 
                    BindMode = BreakpointBindMode.None, 
                    Mode = BreakpointMode.Load,
                    StopWhenHit = true 
                },
                BreakpointDetailDialogMode.Create);
            var message =
                new ShowModalDialogMessage<BreakpointDetailViewModel, SimpleDialogResult>(
                    "Breakpoint properties", 
                    DialogButton.OK | DialogButton.Cancel, 
                    detailViewModel)
                {
                    MinSize = new Size(400, 350),
                    DesiredSize = new Size(600, 350),
                };
            _dispatcher.DispatchShowModalDialog(message);
            var result = await message.Result;
        }
    }

    async Task SaveLocalSettingsAsync(CancellationToken ct = default)
    {
        if (!_suppressLocalPersistence)
        {
            await SaveBreakpointsAsync(ct);
        }
    }

    internal async Task RemoveBreakpointAsync(BreakpointViewModel? breakpoint)
    {
        if (breakpoint is not null)
        {
            try
            {
                await RemoveBreakpointAsync(breakpoint, forceRemove: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove breakpoint");
            }
        }
    }

    internal async Task RemoveAllBreakpointsAsync()
    {
        await RemoveAllBreakpointsAsync(removeFromLocalStorage: true);
    }

    internal async Task ShowBreakpointPropertiesAsync(BreakpointViewModel? breakpoint)
    {
        if (breakpoint is not null)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var detailViewModel = scope.CreateScopedBreakpointDetailViewModel(breakpoint, BreakpointDetailDialogMode.Update);
                var message =
                    new ShowModalDialogMessage<BreakpointDetailViewModel, SimpleDialogResult>(
                        "Breakpoint properties", 
                        DialogButton.OK | DialogButton.Cancel, 
                        detailViewModel);
                _dispatcher.DispatchShowModalDialog(message);
                var result = await message.Result;
            }
        }
    }

    internal Task BreakPointContextAsync(BreakPointContext? context)
    {
        if (context is not null)
        {
            // if (context.Column == BreakPointContextColumn.Binding)
            // {
            //     var binding = context.ViewModel.Bind;
            //     if (binding is BreakpointLineBind lineBind)
            //     {
            //         _dispatcher.Dispatch(new OpenSourceLineNumberFileMessage(
            //             lineBind.File, lineBind.LineNumber + 1, Column: 0, MoveCaret: true));
            //         return Task.CompletedTask;
            //     }
            // }
            // if (ShowBreakpointPropertiesCommand.CanExecute(context.ViewModel))
            // {
            //     ShowBreakpointPropertiesCommand.Execute(context.ViewModel);
            // }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all breakpoints from VICE and locally
    /// </summary>
    /// <param name="removeFromLocalStorage">When true, breakpoints are removed from persistence, left otherwise.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>
    /// <paramref name="removeFromLocalStorage"/> is used when app is cleaning breakpoints but they shouldn't be removed
    /// from persistence.
    /// </remarks>
    internal async Task RemoveAllBreakpointsAsync(bool removeFromLocalStorage, CancellationToken ct = default)
    {
        _suppressLocalPersistence = true;
        try
        {
            while (Breakpoints.Count > 0)
            {
                await RemoveBreakpointAsync(Breakpoints[0], true, ct);
            }
        }
        finally
        {
            _suppressLocalPersistence = false;
            if (removeFromLocalStorage)
            {
                await SaveLocalSettingsAsync(ct);
            }
        }
    }

    void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // switch (e.PropertyName)
        // {
        //     case nameof(ExecutionStatusViewModel.IsDebugging) when !executionStatusViewModel.IsDebugging:
        //     case nameof(ExecutionStatusViewModel.IsDebuggingPaused) when !executionStatusViewModel.IsDebuggingPaused:
        //         ClearHitBreakpoint();
        //         break;
        // }
    }

    internal void ClearHitBreakpoint()
    {
        foreach (var breakpoint in Breakpoints)
        {
            breakpoint.IsCurrentlyHit = false;
        }
    }

    /// <summary>
    /// Removes all breakpoints and reapplies them again.
    /// </summary>
    /// <param name="hasPdbChanged"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>This method is required when lost contact with VICE or when debugging symbols change.</remarks>
    public async Task RearmBreakpoints(bool hasPdbChanged, CancellationToken ct)
    {
        Debug.Assert(Breakpoints.All(bp => !bp.CheckpointNumbers.Any()));
        _suppressLocalPersistence = true;
        try
        {
            foreach (var breakpoint in Breakpoints)
            {
                await _vice.ArmBreakpointAsync(breakpoint, ct);
            }
        }
        finally
        {
            _suppressLocalPersistence = false;
            await SaveLocalSettingsAsync(ct);
        }

        _logger.LogDebug("Checkpoints reapplied");
    }

    public async Task DisarmAllBreakpoints(CancellationToken ct)
    {
        // collects all applied check points in VICE
        var checkpointsList = await _vice.GetCheckpointsListAsync(ct);
        if (checkpointsList is not null)
        {
             // builds map of CheckpointNumber->breakpoint
            var map = new Dictionary<uint, BreakpointViewModel>();
            foreach (var b in Breakpoints)
            {
                foreach (var cn in b.CheckpointNumbers)
                {
                    map.Add(cn, b);
                }
            }
        
            foreach (var ci in checkpointsList.Info)
            {
                if (map.TryGetValue(ci.CheckpointNumber, out var breakpoint))
                {
                    // deletes only those that are part of breakpoints
                    await _vice.DeleteCheckpointAsync(ci.CheckpointNumber, ct);
                    breakpoint.RemoveCheckpointNumber(ci.CheckpointNumber);
                }
                else
                {
                    _logger.Log(LogLevel.Warning, "Breakpoint with checkpoint number {CheckpointNumber} not found when disarming", 
                        ci.CheckpointNumber);
                }
            }
        }
        _breakpointsLinesMap.Clear();
        _breakpointsMap.Clear();
    }

    void UpdateBreakpointDataFromVice(CheckpointInfoResponse checkpointInfo)
    {
        if (_breakpointsMap.TryGetValue(checkpointInfo.CheckpointNumber, out var breakpoint))
        {
            breakpoint.IsCurrentlyHit = checkpointInfo.CurrentlyHit;
            breakpoint.IsEnabled = checkpointInfo.Enabled;
            breakpoint.HitCount = checkpointInfo.HitCount;
            breakpoint.IgnoreCount = checkpointInfo.IgnoreCount;
        }
    }

    public async Task AddBreakpointForLabelAsync(string labelName, string? condition, CancellationToken ct = default)
    {
        // doesn't make sense that there is no line for given label's address
        //await AddBreakpointAsync(true, true, BreakpointMode.Exec, line, lineNumber, file, label, line.Addresses, null);
    }

    /// <summary>
    /// Adds breakpoint linked to line in the file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="lineNumber"></param>
    /// <param name="condition"></param>
    /// <param name="ct"></param>
    public async Task AddLineBreakpointAsync(string filePath, int lineNumber,
        string? condition, CancellationToken ct = default)
    {
        if (!_breakpointsLinesMap.TryGetValue(lineNumber, out var breakpoint))
        {
            var bind = new BreakpointLineBind(filePath, lineNumber, null);
            await AddBreakpointAsync(true, true, BreakpointMode.Exec, bind, ImmutableArray<AddressRange>.Empty, null,
                ct);
        }
    }

    internal async Task ToggleBreakpointEnabledAsync(BreakpointViewModel? breakpoint)
    {
        if (breakpoint is not null)
        {
            var checkpointNumbers = breakpoint.CheckpointNumbers.ToImmutableArray();
            if (!checkpointNumbers.IsEmpty)
            {
                bool allRemoved = true;
                foreach (var cn in checkpointNumbers)
                {
                    bool success = await _vice.ToggleCheckpointAsync(cn, !breakpoint.IsEnabled, CancellationToken.None); 
                    if (!success)
                    {
                        allRemoved = false;
                        _logger.Log(LogLevel.Error, "Failed toggling breakpoint for CheckpointNumber {CheckpointNumber}", cn);
                    }
                }
                if (allRemoved)
                {
                    breakpoint.IsEnabled = !breakpoint.IsEnabled;
                }
            }
        }
    }

    /// <summary>
    /// Adds breakpoint to VICE
    /// </summary>
    /// <param name="stopWhenHit"></param>
    /// <param name="isEnabled"></param>
    /// <param name="mode"></param>
    /// <param name="bind"></param>
    /// <param name="addressRanges"></param>
    /// <param name="condition"></param>
    /// <param name="ct"></param>
    /// <exception cref="Exception"></exception>
    internal async Task AddBreakpointAsync(bool stopWhenHit, bool isEnabled, BreakpointMode mode,
        BreakpointBind bind, ImmutableArray<AddressRange> addressRanges, string? condition,
        CancellationToken ct = default)
    {
        foreach (var range in addressRanges)
        {
            if (range.EndAddress < range.StartAddress)
            {
                throw new Exception($"Invalid breakpoint address range {range.StartAddress} to {range.EndAddress}");
            }
        }
        var breakpointAddressRanges = addressRanges
            .Select(ar => new BreakpointAddressRange(ar.StartAddress, ar.EndAddress))
            .ToHashSet();
        var breakpoint = new BreakpointViewModel(stopWhenHit, isEnabled, mode, bind, condition)
            { AddressRanges = breakpointAddressRanges };
        await AddBreakpointAsync(breakpoint, ct);
    }

    internal async Task AddBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct)
    {
        Breakpoints.Add(breakpoint);
        if (_vice.IsDebugging)
        {
            await _vice.ArmBreakpointAsync(breakpoint, ct);
            // updates checkpoint and lines maps
            foreach (var cp in breakpoint.CheckpointNumbers)
            {
                _breakpointsMap.Add(cp, breakpoint);
            }

            if (breakpoint.Bind is BreakpointLineBind lineBind)
            {
                if (!_breakpointsLinesMap.TryGetValue(lineBind.LineNumber, out var breakpoints))
                {
                    breakpoints = new List<BreakpointViewModel> { breakpoint };
                    _breakpointsLinesMap.Add(lineBind.LineNumber, breakpoints);
                }
                else
                {
                    _breakpointsLinesMap[lineBind.LineNumber].Add(breakpoint);
                }
            }
        }
    }

    public async Task<bool> RemoveBreakpointAsync(BreakpointViewModel breakpoint, bool forceRemove,
        CancellationToken ct = default)
    {
        var checkpointNumbers = breakpoint.CheckpointNumbers.ToImmutableArray();
        if (!checkpointNumbers.IsEmpty)
        {
            bool allRemoved = true;
            foreach (var cn in checkpointNumbers)
            {
                bool success = await _vice.DeleteCheckpointAsync(cn, ct);
                if (!success)
                {
                    // TODO what to do if some fails?
                    allRemoved = false;
                }
                _breakpointsMap.Remove(cn);
            }
            if (allRemoved || forceRemove)
            {
                if (breakpoint.Bind is BreakpointLineBind lineBind)
                {
                    _breakpointsLinesMap.Remove(lineBind.LineNumber);
                }
                Breakpoints.Remove(breakpoint);
                return true;
            }
            return false;
        }
        else
        {
            return Breakpoints.Remove(breakpoint);
        }
    }

    /// <summary>
    /// Updates an existing breakpoint. Will throw if problems with communication or condition fails.
    /// When breakpoint is armed, it will be replaced with new one, nothing is done otherwise.
    /// </summary>
    /// <param name="breakpoint"></param>
    /// <param name="sourceBreakpoint"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>Breakpoint might be a clone and thus equality on <see cref="BreakpointViewModel"/> can not be used.</remarks>
    public async Task UpdateBreakpointAsync(BreakpointViewModel breakpoint, BreakpointViewModel sourceBreakpoint,
        CancellationToken ct = default)
    {
        if (_vice.IsDebugging)
        {
            var checkpointNumbers = breakpoint.CheckpointNumbers.ToImmutableHashSet();
            if (!checkpointNumbers.IsEmpty)
            {
                foreach (uint cn in checkpointNumbers)
                {
                    bool result = await _vice.DeleteCheckpointAsync(cn, ct);
                    if (!result)
                    {
                        _logger.Log(LogLevel.Error, "Failed to remove checkpoint number {CheckpointNumber} from the list", cn);
                    }
                }
            }
        }
        sourceBreakpoint.CopyFrom(breakpoint);
        // clears configuration errors if any
        sourceBreakpoint.HasErrors = false;
        sourceBreakpoint.ErrorText = null;
        // arm breakpoint only when debugging
        if (_vice.IsDebugging)
        {
            await _vice.ArmBreakpointAsync(sourceBreakpoint, ct);
        }
        await SaveLocalSettingsAsync(ct);
    }

    /// <summary>
    /// Creates a <see cref="BreakpointsViewModel"/> for a file exec breakpoint.
    /// </summary>
    /// <param name="b"></param>
    /// <param name="bind"></param>
    /// <returns></returns>
    internal BreakpointViewModel? LoadLineBreakpoint(BreakpointInfo b, BreakpointInfoLineBind bind)
    {
        return new BreakpointViewModel
        {
            StopWhenHit = b.StopWhenHit,
            IsEnabled = b.IsEnabled,
            Mode = b.Mode,
            Condition = b.Condition,
            Bind = new BreakpointLineBind(bind.FilePath, bind.LineNumber, null),
        };
    }

    /// <summary>
    /// Loads an unbound breakpoint - one not tied to either file or label.
    /// </summary>
    /// <param name="b"></param>
    /// <param name="bind"></param>
    /// <returns></returns>
    internal BreakpointViewModel? LoadUnboundBreakpoint(BreakpointInfo b, BreakpointInfoNoBind bind)
    {
        return new BreakpointViewModel
        {
            StopWhenHit = b.StopWhenHit,
            IsEnabled = b.IsEnabled,
            Mode = b.Mode,
            Bind = new BreakpointNoBind(bind.StartAddress, bind.EndAddress),
            Condition = b.Condition,
        };
    }

    public async Task LoadBreakpointsFromSettingsAsync(BreakpointsSettings settings, CancellationToken ct = default)
    {
        foreach (var b in settings.Breakpoints)
        {
            BreakpointViewModel? breakpoint = b.Bind switch
            {
                BreakpointInfoLineBind lineBind => LoadLineBreakpoint(b, lineBind),
                BreakpointInfoNoBind noBind => LoadUnboundBreakpoint(b, noBind),
                _ => throw new Exception($"Unknown breakpoint bind type {b.Bind?.GetType().Name}"),
            };
            if (breakpoint is not null)
            {
                Breakpoints.Add(breakpoint);
            }
        }
    }

    public async Task SaveBreakpointsAsync(CancellationToken ct = default)
    {
        var project = _globals.Project;
        if (project.BreakpointsSettingsPath is not null)
        {
            var items = Breakpoints
                .Where(b => b.Bind is not null)
                .Select(b =>
                    new BreakpointInfo(b.StopWhenHit, b.IsEnabled, b.Mode, b.Condition, b.Bind!.ConvertFromModel())
                ).ToImmutableArray();
            var settings = new BreakpointsSettings(items);
            try
            {
                await _settingsManager.SaveAsync(settings, project.BreakpointsSettingsPath, false, ct);
                _logger.LogDebug("Saved breakpoints settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed saving breakpoints settings");
            }
        }
    }

    public async Task<BreakpointsSettings> LoadBreakpointsAsync(CancellationToken ct = default)
    {
        var project = _globals.Project;
        if (project?.BreakpointsSettingsPath is not null)
        {
            var settings = await _settingsManager.LoadAsync<BreakpointsSettings>(project.BreakpointsSettingsPath, ct);
            if (settings is not null)
            {
                return settings;
            }
        }

        return BreakpointsSettings.Empty;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vice.CheckpointInfoUpdated -= ViceOnCheckpointInfoUpdated;
            _globals.PropertyChanged -= Globals_PropertyChanged;
        }

        base.Dispose(disposing);
    }
}