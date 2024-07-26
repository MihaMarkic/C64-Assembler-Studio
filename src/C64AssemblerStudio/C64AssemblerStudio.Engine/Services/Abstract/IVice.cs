using System.ComponentModel;
using C64AssemblerStudio.Engine.Common;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IVice: INotifyPropertyChanged
{
    bool IsConnected { get; }
    bool IsPaused { get; }
    bool IsDebugging { get; }
    event EventHandler<RegistersEventArgs>? RegistersUpdated;
    Task ConnectAsync(CancellationToken ct = default);
    Task StartDebuggingAsync(CancellationToken ct = default);
    Task StopDebuggingAsync(CancellationToken ct = default);
    Task PauseDebuggingAsync(CancellationToken ct = default);
    Task ContinueAsync(CancellationToken ct = default);
    Task StepIntoAsync(CancellationToken ct = default);
    Task StepOverAsync(CancellationToken ct = default);
}