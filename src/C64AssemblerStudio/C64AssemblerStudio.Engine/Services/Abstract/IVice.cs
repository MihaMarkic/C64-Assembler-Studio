using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Services.Abstract;

public interface IVice: INotifyPropertyChanged, IAsyncDisposable
{
    bool IsConnected { get; }
    bool IsPaused { get; }
    bool IsDebugging { get; }
    bool IsProfiling { get; }
    ViceProfilerState ProfilerState { get; }
    RegistersViewModel Registers { get; }
    /// <summary>
    /// Contains <see cref="BankItem"/> banks grouped by name
    /// </summary>
    FrozenDictionary<string, BankItem> BankItemsByName { get; }
    event EventHandler<RegistersEventArgs>? RegistersUpdated;
    event EventHandler<CheckpointInfoEventArgs>? CheckpointInfoUpdated;
    event EventHandler<MemoryGetEventArgs>? MemoryUpdated;
    Task ConnectAsync(CancellationToken ct = default);
    Task StartDebuggingAsync(CancellationToken ct = default);
    Task StopDebuggingAsync(CancellationToken ct = default);
    Task PauseDebuggingAsync(CancellationToken ct = default);
    /// <summary>
    /// Continues execution.
    /// </summary>
    /// <param name="verifyState">Whether to verify that VICE is paused and only then execute Exit, or always when flag is false.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task ContinueAsync(bool verifyState = true, CancellationToken ct = default);
    Task StepIntoAsync(CancellationToken ct = default);
    Task StepOverAsync(CancellationToken ct = default);
    Task<bool> DeleteCheckpointAsync(uint checkpointNumber, CancellationToken ct = default);
    Task<BreakpointError> ArmBreakpointAsync(BreakpointViewModel breakpoint, bool resumeOnStop, CancellationToken ct);
    Task<bool> ToggleCheckpointAsync(uint checkpointNumber, bool targetEnabledState, CancellationToken ct = default);
    Task<CheckpointListResponse?> GetCheckpointsListAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    /// <summary>
    /// Starts profiling storage and sets <see cref="IsProfiling"/> flag.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<IProfilerStorage> StartProfilerAsync(CancellationToken ct);
	/// <summary>
	/// Starts without profiling and clears previous profiler storage if any.
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
    Task ClearProfilerAsync(CancellationToken ct);
}