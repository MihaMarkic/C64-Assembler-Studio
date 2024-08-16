using System.ComponentModel;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IVice: INotifyPropertyChanged
{
    bool IsConnected { get; }
    bool IsPaused { get; }
    bool IsDebugging { get; }
    RegistersViewModel Registers { get; }
    event EventHandler<RegistersEventArgs>? RegistersUpdated;
    event EventHandler<CheckpointInfoEventArgs>? CheckpointInfoUpdated;
    event EventHandler<MemoryGetEventArgs>? MemoryUpdated;
    Task ConnectAsync(CancellationToken ct = default);
    Task StartDebuggingAsync(CancellationToken ct = default);
    Task StopDebuggingAsync(CancellationToken ct = default);
    Task PauseDebuggingAsync(CancellationToken ct = default);
    Task ContinueAsync(CancellationToken ct = default);
    Task StepIntoAsync(CancellationToken ct = default);
    Task StepOverAsync(CancellationToken ct = default);
    Task<bool> DeleteCheckpointAsync(uint checkpointNumber, CancellationToken ct = default);
    Task<BreakpointError> ArmBreakpointAsync(BreakpointViewModel breakpoint, CancellationToken ct);
    Task<bool> ToggleCheckpointAsync(uint checkpointNumber, bool targetEnabledState, CancellationToken ct = default);
    Task<CheckpointListResponse?> GetCheckpointsListAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}