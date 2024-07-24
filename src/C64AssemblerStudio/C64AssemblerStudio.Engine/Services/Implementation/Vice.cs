using System.Diagnostics;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class Vice: NotifiableObject, IVice
{
    private readonly ILogger<Vice> _logger;
    private readonly IViceBridge _bridge;
    private readonly Globals _globals;
    private readonly IDispatcher _dispatcher;
    private Process? _process;
    public bool IsConnected { get; private set; }
    public bool IsDebugging { get; private set; }
    public bool IsPaused { get; private set; }

    public Vice(ILogger<Vice> logger, IViceBridge bridge, Globals globals, IDispatcher dispatcher)
    {
        _logger = logger;
        _bridge = bridge;
        _globals = globals;
        _dispatcher = dispatcher;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        const string title = "Connecting";
        if (IsConnected)
        {
            return;
        }

        _process = StartVice();
        if (_process is null)
        {
            _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, title, "Failed to start debugging"));
            return;
        }
        _process.Exited += ProcessOnExited;
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
}