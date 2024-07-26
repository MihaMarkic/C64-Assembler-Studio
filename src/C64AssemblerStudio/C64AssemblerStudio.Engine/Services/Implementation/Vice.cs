using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class Vice: NotifiableObject, IVice
{
    private readonly ILogger<Vice> _logger;
    private readonly IViceBridge _bridge;
    private readonly Globals _globals;
    private readonly IDispatcher _dispatcher;
    private readonly RegistersViewModel _registers;
    private Process? _process;
    public event EventHandler<RegistersEventArgs>? RegistersUpdated; 
    public bool IsConnected { get; private set; }
    public bool IsDebugging { get; private set; }
    public bool IsPaused { get; private set; }

    public Vice(ILogger<Vice> logger, IViceBridge bridge, Globals globals, IDispatcher dispatcher,
        RegistersViewModel registers)
    {
        _logger = logger;
        _bridge = bridge;
        _globals = globals;
        _dispatcher = dispatcher;
        _registers = registers;
        _bridge.ConnectedChanged += BridgeOnConnectedChanged;
        _bridge.ViceResponse += BridgeOnViceResponse;
        _bridge.Start();
    }

    private async void BridgeOnViceResponse(object? sender, ViceResponseEventArgs e)
    {
        switch (e.Response)
        {
            case RegistersResponse registersResponse:
                await _registers.UpdateAsync(registersResponse, CancellationToken.None);
                break;
        }
    }

    private async Task InitRegistersMappingAsync(CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory), resumeOnStopped: true);
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        if (response is not null)
        {
            _registers.Init(response);
        }
        else
        {
            _logger.LogError("Failed retrieving registers mapping");
        }
    }

    private void BridgeOnConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
        IsConnected = e.IsConnected;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        const string title = "Connecting";
        if (_process is null)
        {
            _process = StartVice();
            if (_process is null)
            {
                _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, title, "Failed to start debugging"));
                return;
            }

            _process.Exited += ProcessOnExited;
        }

        if (!IsConnected)
        {
            await _bridge.WaitForConnectionStatusChangeAsync(ct);
            await InitRegistersMappingAsync(ct);
        }
    }

    public async Task StartDebuggingAsync(CancellationToken ct = default)
    {
        if (IsDebugging)
        {
            _logger.LogWarning("Can't start debugging - already debugging");
            return;
        }
        IsPaused = false;
        var command = _bridge.EnqueueCommand(
            new AutoStartCommand(runAfterLoading: true, 0, _globals.Project.FullPrgPath!),
            resumeOnStopped: false);
        await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
        IsDebugging = true;
    }
    public async Task StepIntoAsync(CancellationToken ct = default)
    {
        if (VerifyIsPaused("step into"))
        {
            IsPaused = false;
            try
            {
                ushort instructionsNumber = 1;
                var command =
                    _bridge.EnqueueCommand(new AdvanceInstructionCommand(StepOverSubroutine: false,
                        instructionsNumber));
                await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
            }
            finally
            {
                IsPaused = true;
            }
        }
    }

    public async Task StepOverAsync(CancellationToken ct = default)
    {
        if (VerifyIsPaused("step over"))
        {
            IsPaused = false;
            try
            {
                ushort instructionsNumber = 1;
                var command =
                    _bridge.EnqueueCommand(new AdvanceInstructionCommand(StepOverSubroutine: true, instructionsNumber));
                await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
            }
            finally
            {
                IsPaused = true;
            }
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
                var command = _bridge.EnqueueCommand(new ExitCommand(),  resumeOnStopped: true);
                await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
            }
            _logger.LogInformation("Debugging stopped");
        }
        IsDebugging = false;
        IsPaused = false;
    }

    public async Task PauseDebuggingAsync(CancellationToken ct = default)
    {
        if (VerifyIsDebugging("pause"))
        {
            var command = _bridge.EnqueueCommand(new PingCommand(), resumeOnStopped: false);
            await command.Response;
            IsPaused = true;
        }
    }
    public async Task ExitViceMonitorAsync(CancellationToken ct = default)
    {
        var command = _bridge.EnqueueCommand(new ExitCommand(), resumeOnStopped: false);
        await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
    }
    private void ProcessOnExited(object? sender, EventArgs e)
    {
        _process.ValueOrThrow().Exited -= ProcessOnExited;
    }

    internal Process? StartVice()
    {
        string? realVicePath = _globals.Settings.RealVicePath;
        if (!string.IsNullOrWhiteSpace(realVicePath))
        {
            string path = Path.Combine(realVicePath, "x64sc.exe");
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
            _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE", "VICE path is not set in settings"));
            return null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ = _bridge.StopAsync(waitForQueueToProcess:false);
            _bridge.ConnectedChanged -= BridgeOnConnectedChanged;
            _bridge.ViceResponse -= BridgeOnViceResponse;
        }
        base.Dispose(disposing);
    }
}