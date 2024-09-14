using System.Collections.Frozen;
using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class Vice : NotifiableObject, IVice
{
    private readonly ILogger<Vice> _logger;
    private readonly IViceBridge _bridge;
    private readonly Globals _globals;
    private readonly IDispatcher _dispatcher;
    private readonly TaskFactory _uiFactory;
    public RegistersViewModel Registers { get; }
    public ViceMemoryViewModel Memory { get; }
    public CallStackViewModel CallStack { get; }

    /// <inheritdoc cref="IVice.BankItemsByName"/>
    public FrozenDictionary<string, BankItem> BankItemsByName { get; private set; } =
        FrozenDictionary<string, BankItem>.Empty;

    /// <summary>
    /// Contains <see cref="BankItem"/> banks grouped by BankId
    /// </summary>
    /// <remarks>More than one bank can have same id - because aliases</remarks>
    public FrozenDictionary<ushort, FrozenSet<BankItem>> BankItemsById { get; private set; } =
        FrozenDictionary<ushort, FrozenSet<BankItem>>.Empty;

    private Process? _process;
    public event EventHandler<RegistersEventArgs>? RegistersUpdated;
    public event EventHandler<CheckpointInfoEventArgs>? CheckpointInfoUpdated;
    public event EventHandler<MemoryGetEventArgs>? MemoryUpdated;
    public bool IsConnected { get; private set; }
    public bool IsDebugging { get; private set; }
    public bool IsPaused { get; private set; }

    public Vice(ILogger<Vice> logger, IViceBridge bridge, Globals globals, IDispatcher dispatcher,
        RegistersViewModel registers, ViceMemoryViewModel viceMemory, CallStackViewModel callStack)
    {
        _logger = logger;
        _bridge = bridge;
        _globals = globals;
        _dispatcher = dispatcher;
        Registers = registers;
        Memory = viceMemory;
        CallStack = callStack;
        
        _uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        _bridge.ConnectedChanged += BridgeOnConnectedChanged;
        _bridge.ViceResponse += BridgeOnViceResponse;
        _bridge.Start();
    }

    private void OnRegistersUpdated(RegistersEventArgs e) => RegistersUpdated?.Invoke(this, e);
    private void OnCheckpointInfoUpdated(CheckpointInfoEventArgs e) => CheckpointInfoUpdated?.Invoke(this, e);
    private void OnMemoryUpdated(MemoryGetEventArgs e) => MemoryUpdated?.Invoke(this, e);
    private bool _retrieveMemory;
    private async void BridgeOnViceResponse(object? sender, ViceResponseEventArgs e)
    {
        // handle all responses in UI thread
        await _uiFactory.StartNewTyped(async r =>
        {
            _logger.LogDebug("Got {Response}", e.Response.GetType().Name);
            switch (r!)
            {
                case RegistersResponse registersResponse:
                    Registers.UpdateRegistersFromResponse(registersResponse);
                    OnRegistersUpdated(new RegistersEventArgs(registersResponse));
                    break;
                case CheckpointInfoResponse checkpointInfoResponse:
                    OnCheckpointInfoUpdated(new CheckpointInfoEventArgs((checkpointInfoResponse)));
                    _retrieveMemory = true;
                    break;
                case ResumedResponse:
                    IsPaused = false;
                    break;
                case StoppedResponse:
                    if (_retrieveMemory)
                    {
                        try
                        {
                            await GetMemoryAsync(CancellationToken.None);
                            CallStack.Update();
                        }
                        finally
                        {
                            _retrieveMemory = false;
                        }
                    }
                    IsPaused = true;
                    break;
            }
        }, e.Response, CancellationToken.None);
    }

    private async Task GetMemoryAsync(CancellationToken ct)
    {
        _logger.LogDebug("Requesting memory");
        var command = _bridge.EnqueueCommand(
            new MemoryGetCommand(0, 0, ushort.MaxValue-1, MemSpace.MainMemory, 0),
            false);
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        _logger.LogDebug("Got memory");
        OnMemoryUpdated(new(response));
        Memory.GetSnapshot(response.ValueOrThrow());
    }

    private async Task InitRegistersMappingAsync(CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory), resumeOnStopped: true);
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        if (response is not null)
        {
            Registers.Init(response);
        }
        else
        {
            _logger.LogError("Failed retrieving registers mapping");
        }
    }

    private async Task InitAvailableBanksAsync(CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new BanksAvailableCommand(), resumeOnStopped: true);
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        if (response is not null)
        {
            BankItemsById = response.Banks.GroupBy(b => b.BankId)
                .ToFrozenDictionary(g => g.Key, g => g.ToFrozenSet());
            BankItemsByName = response.Banks.ToFrozenDictionary(bi => bi.Name);
        }
        else
        {
            _logger.LogError("Failed retrieving available banks");
        }
    }

    private void BridgeOnConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
        IsConnected = e.IsConnected;
        _logger.LogDebug("VICE changed to {connected}", e.IsConnected);
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        const string title = "Connecting";
        if (_process is null)
        {
            _process = StartVice();
            if (_process is null)
            {
                await _dispatcher.DispatchAsync(new ErrorMessage(ErrorMessageLevel.Error, title, "Failed to start debugging"), ct: ct);
                return;
            }

            _process.Exited += ProcessOnExited;
        }

        if (!IsConnected)
        {
            await _bridge.WaitForConnectionStatusChangeAsync(ct);
        }
        await InitRegistersMappingAsync(ct);
        await InitAvailableBanksAsync(ct);
    }

    public async Task StartDebuggingAsync(CancellationToken ct = default)
    {
        if (IsDebugging)
        {
            _logger.LogWarning("Can't start debugging - already debugging");
            return;
        }

        IsDebugging = true;
        var command = _bridge.EnqueueCommand(
            new AutoStartCommand(runAfterLoading: true, 0, _globals.Project.FullPrgPath!),
            resumeOnStopped: false);
        await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
    }

    public async Task StepIntoAsync(CancellationToken ct = default)
    {
        if (VerifyIsPaused("step into"))
        {
            _retrieveMemory = true;
            ushort instructionsNumber = 1;
            var command =
                _bridge.EnqueueCommand(new AdvanceInstructionCommand(StepOverSubroutine: false,
                    instructionsNumber));
            await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        }
    }

    public async Task StepOverAsync(CancellationToken ct = default)
    {
        if (VerifyIsPaused("step over"))
        {
            _retrieveMemory = true;
            ushort instructionsNumber = 1;
            var command =
                _bridge.EnqueueCommand(new AdvanceInstructionCommand(StepOverSubroutine: true, instructionsNumber));
            await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        }
    }

    public async Task ContinueAsync(CancellationToken ct = default)
    {
        if (VerifyIsPaused("continue"))
        {
            await ExitViceMonitorAsync(ct);
        }

        IsPaused = false;
    }

    bool VerifyIsPaused(string verb)
    {
        if (!IsDebugging)
        {
            _logger.LogWarning($"Can't {verb} - not debugging");
            return false;
        }
        else if (!IsPaused)
        {
            _logger.LogWarning($"Can't {verb} - already running");
            return false;
        }

        return true;
    }

    bool VerifyIsDebugging(string verb)
    {
        if (!IsDebugging)
        {
            _logger.LogWarning($"Can't {verb} - not debugging");
            return false;
        }
        else if (IsPaused)
        {
            _logger.LogWarning($"Can't {verb} - already paused");
            return false;
        }

        return true;
    }

    public async Task StopDebuggingAsync(CancellationToken ct = default)
    {
        if (_bridge.IsConnected)
        {
            if (_globals.Settings.ResetOnStop)
            {
                _logger.LogInformation("Stopping debugging with reset on stop");
                var command = _bridge.EnqueueCommand(new ResetCommand(ResetMode.Soft), resumeOnStopped: false);
                await command.Response;
            }
            else
            {
                _logger.LogInformation("Stopping debugging with exit");
                var command = _bridge.EnqueueCommand(new ExitCommand(), resumeOnStopped: true);
                await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
            }

            _logger.LogInformation("Debugging stopped");
        }

        IsDebugging = false;
    }

    public async Task PauseDebuggingAsync(CancellationToken ct = default)
    {
        if (VerifyIsDebugging("pause"))
        {
            _retrieveMemory = true;
            var command = _bridge.EnqueueCommand(new PingCommand(), resumeOnStopped: false);
            await command.Response;
        }
    }

    public async Task ExitViceMonitorAsync(CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new ExitCommand(), resumeOnStopped: false);
        await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
    }

    private async void ProcessOnExited(object? sender, EventArgs e)
    {
        _process.ValueOrThrow().Exited -= ProcessOnExited;
        _process = null;
    }

    internal Process? StartVice()
    {
        string? realVicePath = _globals.Settings.RealVicePath;
        if (!string.IsNullOrWhiteSpace(realVicePath))
        {
            string path = Path.Combine(realVicePath, OsDependent.ViceExeName);
            try
            {
                string arguments = _globals.Settings.BinaryMonitorArgument;
                var process = Process.Start(path, arguments);
                process.EnableRaisingEvents = true;
                return process;
            }
            catch (Exception ex)
            {
                _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE", ex.Message));
                return null;
            }
        }
        else
        {
            _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE",
                "VICE path is not set in settings"));
            return null;
        }
    }

    public async Task<bool> DeleteCheckpointAsync(uint checkpointNumber, CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new CheckpointDeleteCommand(checkpointNumber),
            resumeOnStopped: true);
        var result = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        return result is not null;
    }

    public async Task<BreakpointError> ArmBreakpointAsync(BreakpointViewModel breakpoint, bool resumeOnStop,
        CancellationToken ct)
    {
        // if (breakpoint.HasErrors)
        // {
        //     _logger.Log(LogLevel.Warning, "Breakpoint has errors {ErrorText} on re-arm", breakpoint.ErrorText);
        //     return false;
        // }

        if (breakpoint.AddressRanges is null)
        {
            _logger.LogError("Breakpoint doesn't have address range");
            return BreakpointError.NoAddressRange;
        }
        breakpoint.ClearCheckpointNumbers();
        foreach (var addressRange in breakpoint.AddressRanges)
        {
            var checkpointSetCommand = _bridge.EnqueueCommand(
                new CheckpointSetCommand(addressRange.Start, addressRange.End, breakpoint.StopWhenHit,
                    breakpoint.IsEnabled, breakpoint.Mode.ToCpuOperation(), false));
            var checkpointSetResponse = await checkpointSetCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger,
                checkpointSetCommand, ct: ct);
            if (checkpointSetResponse is not null)
            {
                // apply condition to checkpoint if any
                if (!string.IsNullOrWhiteSpace(breakpoint.Condition))
                {
                    var conditionSetCommand = _bridge.EnqueueCommand(
                        new ConditionSetCommand(checkpointSetResponse.CheckpointNumber, breakpoint.Condition));
                    var conditionSetResponse = await conditionSetCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger,
                        conditionSetCommand, ct: ct);
                    // in case condition set fails, remove the checkpoint
                    if (conditionSetResponse is null)
                    {
                        _logger.LogError("Failed setting {Condition}", breakpoint.Condition);
                        var checkpointDeleteCommand = _bridge.EnqueueCommand(
                            new CheckpointDeleteCommand(checkpointSetResponse.CheckpointNumber),
                                resumeOnStopped: true);
                        await checkpointDeleteCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, checkpointDeleteCommand, ct: ct);
                        return BreakpointError.InvalidConditon;
                    }
                }
                breakpoint.AddCheckpointNumber(addressRange, checkpointSetResponse.CheckpointNumber);
            }
        }

        if (resumeOnStop && IsPaused)
        {
            var command = _bridge.EnqueueCommand(new ExitCommand(), resumeOnStopped: true);
            await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        }

        return BreakpointError.None;
    }

    public async Task<bool> ToggleCheckpointAsync(uint checkpointNumber, bool targetEnabledState, CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new CheckpointToggleCommand(checkpointNumber, targetEnabledState), resumeOnStopped: true);
        var result = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        return result?.ErrorCode == ErrorCode.OK;
    }

    public async Task<CheckpointListResponse?> GetCheckpointsListAsync(CancellationToken ct = default)
    {
        var checkpointsListCommand = _bridge.EnqueueCommand(new CheckpointListCommand(), resumeOnStopped: true);
        return await checkpointsListCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, checkpointsListCommand, ct: ct);
    }

    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(IsConnected) when !IsConnected:
                IsPaused = false;
                IsDebugging = false;
                break;
        }
        base.OnPropertyChanged(name);
    }
    
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Disconnecting");
        await _bridge.StopAsync(waitForQueueToProcess: false);
        _logger.LogDebug("Bridge stopped");
        _bridge.ConnectedChanged -= BridgeOnConnectedChanged;
        _bridge.ViceResponse -= BridgeOnViceResponse;
        if (_process is not null)
        {
            _process.Kill();
            _logger.LogDebug("VICE process closed");
        }
        _logger.LogDebug("Disconnected");
    }
}