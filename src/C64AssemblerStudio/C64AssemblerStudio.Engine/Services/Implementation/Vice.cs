using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public enum ViceProfilerState
{
	None,
	Initializing,
	Collecting,

	/// <summary>
	/// When stop debugging is issued.
	/// </summary>
	Stopping,
	Stopped,
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Vice : NotifiableObject, IVice
{
	private readonly ILogger<Vice> _logger;
	private readonly IViceBridge _bridge;
	private readonly IGlobals _globals;
	private readonly IDispatcher _dispatcher;
	private readonly TaskFactory _uiFactory;
	private readonly IOSDependent _osDependent;
	private readonly IServiceScope _serviceScope;
	private readonly ResponseBuilder _responseBuilder;

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
	private IProfilerStorage? _profilerStorage;
	public event EventHandler<RegistersEventArgs>? RegistersUpdated;
	public event EventHandler<CheckpointInfoEventArgs>? CheckpointInfoUpdated;
	public event EventHandler<MemoryGetEventArgs>? MemoryUpdated;
	/// <inheritdoc/>
	public event EventHandler? ReturnAddressHit; 

	/// <summary>
	/// This event is raised when new batch of CpuHistory is available.
	/// </summary>
	/// <remarks>
	/// Used when profiling only.
	/// </remarks>
	public event EventHandler<CpuHistoryEventArgs>? CpuHistoryUpdated;

	public bool IsConnected { get; private set; }
	public bool IsDebugging { get; private set; }
	public bool IsPaused { get; private set; }

	/// <summary>
	/// Sets whether program is profiled or not.
	/// </summary>
	/// <remarks>
	/// Has only effect before starting debugging.
	/// </remarks>
	public bool IsProfiling { get; private set; }

	public ViceProfilerState ProfilerState { get; private set; } = ViceProfilerState.None;
	/// <summary>
	/// When profiling this is the current CpuHistory command being executed.
	/// </summary>
	private AdvanceInstructionCommand? _currentProfileAdvanceCommand;

	private readonly BufferBlock<ViceResponse> _responseBuffer = new();
	private readonly Task _bridgeResponseProcessorTask;
	private readonly CancellationTokenSource _bridgeResponseProcessorCts = new();
	private bool _retrieveMemory;
	/// <summary>
	/// First AdvanceInstruction has to be stopped only on second unbound Stopped message
	/// </summary>
	private bool _isFirstProfilerStop;
	/// <summary>
	/// This is the address from where program was called.
	/// </summary>
	/// <remarks>Set after program starts. Not thread safe, use <see cref="Interlocked"/> access.</remarks>
	private ushort? _returnAddress;

	public Vice(ILogger<Vice> logger, IViceBridge bridge, IGlobals globals, IDispatcher dispatcher,
		RegistersViewModel registers, ViceMemoryViewModel viceMemory, CallStackViewModel callStack,
		IOSDependent osDependent, IServiceScope serviceScope, ResponseBuilder responseBuilder)
	{
		_logger = logger;
		_bridge = bridge;
		_globals = globals;
		_dispatcher = dispatcher;
		_osDependent = osDependent;
		_serviceScope = serviceScope;
		_responseBuilder = responseBuilder;
		Registers = registers;
		Memory = viceMemory;
		CallStack = callStack;

		_uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());

		var token = _bridgeResponseProcessorCts!.Token;
		// start unbound responses sequential processor
		var task = Task.Factory.StartNew(
			() => BridgeResponseProcessor(_responseBuffer, token),
			cancellationToken: token,
			creationOptions: TaskCreationOptions.LongRunning,
			scheduler: TaskScheduler.Default);
		_bridgeResponseProcessorTask = task.Unwrap();

		_bridge.ConnectedChanged += BridgeOnConnectedChanged;
		_bridge.ViceResponse += BridgeOnViceResponse;
		_bridge.Start(_globals.Settings.ViceMonitorPort);
	}

	private void OnRegistersUpdated(RegistersEventArgs e) => RegistersUpdated?.Invoke(this, e);
	private void OnCheckpointInfoUpdated(CheckpointInfoEventArgs e) => CheckpointInfoUpdated?.Invoke(this, e);
	private void OnMemoryUpdated(MemoryGetEventArgs e) => MemoryUpdated?.Invoke(this, e);
	private void OnCpuHistoryUpdated(CpuHistoryEventArgs e) => CpuHistoryUpdated?.Invoke(this, e);
	private void OnReturnAddressHit(EventArgs e) => ReturnAddressHit?.Invoke(this, e);

	/// <inheritdoc/>
	public async Task<IProfilerStorage> StartProfilerAsync(CancellationToken ct)
	{
		if (_profilerStorage is not null)
		{
			_logger.LogDebug("Previous profiler storage still running, waiting for flush");
			await _profilerStorage.FlushAsync(ct);
		}

		// project has to exist at this point
		_profilerStorage = _serviceScope.GetProfilerStorage(_globals.Project.Directory!);
		await _profilerStorage.InitAsync(ct);
		ProfilerState = ViceProfilerState.None;
		IsProfiling = true;
		_isFirstProfilerStop = true;
		return _profilerStorage;
	}

	/// <inheritdoc/>
	public async Task ClearProfilerAsync(CancellationToken ct)
	{
		if (_profilerStorage is not null)
		{
			_logger.LogDebug("Previous profiler storage still running, waiting for flush");
			await _profilerStorage.FlushAsync(ct);
		}

		_profilerStorage = null;
		IsProfiling = false;
	}

	private void BridgeOnViceResponse(object? sender, ViceResponseEventArgs e)
	{
		_ = _responseBuffer.SendAsync(e.Response);
	}

	/// <summary>
	/// Sequentially schedules unbound VICE responses.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="ct"></param>
	private async Task BridgeResponseProcessor(IReceivableSourceBlock<ViceResponse> source, CancellationToken ct)
	{
		while (await source.OutputAvailableAsync(ct).ConfigureAwait(false))
		{
			try
			{
				if (source.TryReceive(out var response))
				{
					await _uiFactory.StartNewTyped(BridgeResponseProcessor, response, ct);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogDebug("Exiting loop because cancelled");
			}
		}

		_logger.LogDebug("Loop done");
	}

	/// <summary>
	/// Sequential processing of VICE unbound responses
	/// </summary>
	/// <param name="response"></param>
	/// <param name="ct"></param>
	private async Task BridgeResponseProcessor(ViceResponse response, CancellationToken ct)
	{
		if (IsProfiling)
		{
			await BridgeResponseProcessorForProfilingAsync(response, ct);
		}
		else
		{
			await BridgeResponseProcessorForDebuggingAsync(response, ct);
		}
	}

	private async Task BridgeResponseProcessorForDebuggingAsync(ViceResponse response, CancellationToken ct)
	{
		_logger.LogDebug("Got {Response}", response.GetType().Name);
		switch (response!)
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
						await GetMemoryAsync(ct);
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
	}
	private async Task BridgeResponseProcessorForProfilingAsync(ViceResponse response, CancellationToken ct)
	{
		switch (response)
		{
			// the only StoppedResponse should be the one happening after CheckpointInfoResponse
			case StoppedResponse:
				// _logger.LogDebug("Got StoppedResponse");
				switch (ProfilerState)
				{
					case ViceProfilerState.Collecting:
					case ViceProfilerState.Stopping:
						if (IsDebugging)
						{
							switch (ProfilerState)
							{
								case ViceProfilerState.Collecting:
									// ignore only first stop - this is caused by first AdvanceInstructions
									if (!_isFirstProfilerStop)
									{
										_logger.LogDebug("Collecting CPU history");
										if (await CollectCpuHistoryAsync(ct))
										{
											_logger.LogDebug("Advancing");
											_ = AdvanceProfilingAsync(ct);
										}
										else
										{
											_logger.LogInformation("Return address detected, will stop profiling");
											ProfilerState = ViceProfilerState.Stopped;
											OnReturnAddressHit(EventArgs.Empty);
										}
									}
									else
									{
										_isFirstProfilerStop = false;
									}

									break;
								case ViceProfilerState.Stopping:
									_logger.LogDebug($"Stopped");
									ProfilerState = ViceProfilerState.Stopped;
									break;
							}
						}

						break;
				}

				break;
			default:
				// _logger.LogDebug("Got {Response} but it will be ignored due profiling",
				// 	e.Response.GetType().Name);
				break;
		}

		return;
	}

	/// <summary>
	/// Collects CPU History data and sends it to storage processor.
	/// </summary>
	/// <param name="ct"></param>
	/// <returns>True when return address is not found, false when found and profiling can stop.</returns>
	private async ValueTask<bool> CollectCpuHistoryAsync(CancellationToken ct)
	{
		var cpuHistoryCommand = new CpuHistoryCommand(MemSpace.MainMemory, Settings.CpuHistoryLines);
		_bridge.EnqueueCommand(cpuHistoryCommand);
		var result =
			await cpuHistoryCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, cpuHistoryCommand,
				ct: ct);
		if (result is not null)
		{
			var storeTask = _profilerStorage!.StoreChunkAsync(result);
			bool hasReturnAddress = false;
			int? pcRegisterIndex = Registers.Current.PC;
			// both return address must exist and PC register index has to be known to determine whether returnAddress 
			// is contained in block
			if (_returnAddress is not null && pcRegisterIndex is not null)
			{
				hasReturnAddress = await HasReturnAddressAsync(result, _returnAddress.Value, pcRegisterIndex.Value, ct);
			}

			await storeTask;
			return hasReturnAddress;
		}
		else
		{
			_logger.LogError("Could not get CPU history response");
			throw new Exception("Failed retrieving  CPU history");
			// TODO stop profiling
		}

		return true;
	}

	private async ValueTask<bool> HasReturnAddressAsync(CpuHistoryBufferResponse response, uint returnAddress,
		int pcRegisterIndex, CancellationToken ct)
	{
		return await Task.Factory.StartNew(o =>
		{
			
			var (r, ra, pcIndex) = (HasReturnAddressItem)o!;
			var parsed = r.ParseContent(_responseBuilder);
			var hasReturnAddress = parsed.Any(i => i.RegisterItems[pcIndex].RegisterValue == ra);
			return hasReturnAddress;
		}, new HasReturnAddressItem(response, returnAddress, pcRegisterIndex) , ct);
	}
	private record HasReturnAddressItem(CpuHistoryBufferResponse Response, uint ReturnAddress, int PcRegisterIndex);

	private async Task GetMemoryAsync(CancellationToken ct)
	{
		_logger.LogDebug("Requesting memory");
		var command = _bridge.EnqueueCommand(
			new MemoryGetCommand(0, 0, ushort.MaxValue - 1, MemSpace.MainMemory, 0),
			false);
		var response = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
		_logger.LogDebug("Got memory");
		OnMemoryUpdated(new(response));
		Memory.GetSnapshot(response.ValueOrThrow());
	}

	private async Task InitRegistersMappingAsync(CancellationToken ct = default)
	{
		var command = _bridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory));
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
		var command = _bridge.EnqueueCommand(new BanksAvailableCommand());
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
				await _dispatcher.DispatchAsync(
					new ErrorMessage(ErrorMessageLevel.Error, title, "Failed to start debugging"), ct: ct);
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
		if (IsProfiling)
		{
			_logger.LogInformation("Debugging with profiler enabled");
		}

		var programStartCheckpoint = await InsertProgramStartCheckpointAsync(ct);
		if (programStartCheckpoint is null)
		{
			return;
		}

		var command = _bridge.EnqueueCommand(
			new AutoStartCommand(runAfterLoading: true, 0, _globals.Project.FullPrgPath!),
			resumeOnStopped: false);
		await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);

		_logger.LogDebug("Waiting for start program checkpoint to be hit");
		await _bridge.WaitForUnboundResponseAsync<CheckpointInfoResponse>(
			_logger,
			(r) => r.CheckpointNumber == programStartCheckpoint.CheckpointNumber,
			waitForStop: true, ct);
		_logger.LogDebug("Start program checkpoint hit");
		await RetrieveReturnAddressAsync(ct);
	}

	/// <inheritdoc/>
	public async Task StartProfilingAsync(CancellationToken ct = default)
	{
		if (IsDebugging)
		{
			_logger.LogWarning("Can't start debugging - already debugging");
			return;
		}

		IsDebugging = true;

		var programStartCheckpoint = await InsertProgramStartCheckpointAsync(ct);
		if (programStartCheckpoint is null)
		{
			return;
		}

		var command = _bridge.EnqueueCommand(
			new AutoStartCommand(runAfterLoading: true, 0, _globals.Project.FullPrgPath!),
			resumeOnStopped: false);
		await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);

		_logger.LogDebug("Waiting for start program checkpoint to be hit");
		await _bridge.WaitForUnboundResponseAsync<CheckpointInfoResponse>(
			_logger,
			(r) => r.CheckpointNumber == programStartCheckpoint.CheckpointNumber,
			waitForStop: true, ct);
		_logger.LogDebug("Start program checkpoint hit");
		ProfilerState = ViceProfilerState.Collecting;
		await RetrieveReturnAddressAsync(ct);

		_ = AdvanceProfilingAsync(ct);
	}

	/// <summary>
	/// Extract calling address from stack and stores it into <see cref="_returnAddress"/>.
	/// </summary>
	/// <param name="ct"></param>
	/// <remarks>VICE is stopped at this point.</remarks>
	private async ValueTask RetrieveReturnAddressAsync(CancellationToken ct)
	{
		var sp = Registers.Current.SP;
		if (sp is null)
		{
			_logger.LogWarning("Couldn't retrieve return address - SP is null");
			return;
		}

		var memoryStart = (ushort)(0x0100 + sp + 1);
		var memoryEnd = (ushort)(memoryStart + 2);
		// extract just the amount of stack we need (SP + 2 bytes)
		var memoryCommand =
			_bridge.EnqueueCommand(new MemoryGetCommand(0, memoryStart, memoryEnd, MemSpace.MainMemory, 0));
		var memoryResponse = memoryCommand.Response;
		await memoryResponse.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, memoryCommand, ct: ct);
		if (!memoryResponse.Result.IsSuccess)
		{
			_logger.LogWarning("Failed to read memory from {Start} to {End} for retrieving return address",
				memoryStart, memoryEnd);
			return;
		}

		if (memoryResponse.Result.Response?.Memory is null)
		{
			_logger.LogWarning("Failed to obtain memory from {Start} to {End} for retrieving return address",
				memoryStart, memoryEnd);
			return;
		}

		_returnAddress = (ushort)(BitConverter.ToUInt16(memoryResponse.Result.Response.Memory.Value.Data) + 1);
	}

	private async Task AdvanceProfilingAsync(CancellationToken ct)
	{
		_currentProfileAdvanceCommand =
			new AdvanceInstructionCommand(StepOverSubroutine: false, Settings.CpuHistoryLines);
		_bridge.EnqueueCommand(_currentProfileAdvanceCommand, resumeOnStopped: true);
		await _currentProfileAdvanceCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher,
			_logger,
			_currentProfileAdvanceCommand, ct: ct);
	}

	private async Task<CheckpointInfoResponse?> InsertProgramStartCheckpointAsync(CancellationToken ct)
	{
		var address = _globals.Project.ProgramEntryAddress;
		if (address is null)
		{
			_logger.LogWarning("Can't start debugging - no program entry address");
			return null;
		}

		_logger.LogDebug("Inserting start program checkpoint at address {Address:X4}", address);
		var checkpointSetCommand = _bridge.EnqueueCommand(
			new CheckpointSetCommand(address.Value, address.Value, true, true, CpuOperation.Exec, true),
			resumeOnStopped: true);
		var checkpointSetResponse = await checkpointSetCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher,
			_logger,
			checkpointSetCommand, ct: ct);
		if (checkpointSetResponse is null)
		{
			_logger.LogWarning("Failed setting initial checkpoint in VICE");
			return null;
		}

		return checkpointSetResponse;
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

	/// <inheritdoc/>
	public async Task ContinueAsync(bool verifyState = true, CancellationToken ct = default)
	{
		if (verifyState && VerifyIsPaused("continue") || !verifyState)
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
			if (IsProfiling)
			{
				if (ProfilerState != ViceProfilerState.Stopped)
				{
					ProfilerState = ViceProfilerState.Stopping;
				}

				_logger.LogInformation($"Waiting for ViceProfilerState.Stopped");
				await WaitForProfilerStateValueAsync(ViceProfilerState.Stopped, ct);
				_logger.LogInformation("Flushing profiler");
				await _profilerStorage!.FlushAsync(ct);
			}

			if (_globals.Settings.ResetOnStop)
			{
				_logger.LogInformation("Stopping debugging with reset on stop");
				var command = _bridge.EnqueueCommand(new ResetCommand(ResetMode.Soft));
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

	/// <summary>
	/// Awaits desired <see cref="ProfilerState"/>.
	/// </summary>
	/// <param name="state"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	internal Task WaitForProfilerStateValueAsync(ViceProfilerState state, CancellationToken ct = default)
	{
		if (ProfilerState == state)
		{
			return Task.CompletedTask;
		}

		var cts = new TaskCompletionSource();
		PropertyChangedEventHandler? handler = null;
		handler = (sender, args) =>
		{
			if (args.PropertyName == nameof(ProfilerState))
			{
				if (ProfilerState == state)
				{
					PropertyChanged -= handler;
					cts.TrySetResult();
				}
			}
		};
		PropertyChanged += handler;
		ct.Register(() =>
		{
			PropertyChanged -= handler;
			cts.TrySetCanceled();
		});
		return cts.Task;
	}

	public async Task PauseDebuggingAsync(CancellationToken ct = default)
	{
		if (VerifyIsDebugging("pause"))
		{
			_retrieveMemory = true;
			var command = _bridge.EnqueueCommand(new PingCommand());
			await command.Response;
		}
	}

	public async Task ExitViceMonitorAsync(CancellationToken ct = default)
	{
		var command = _bridge.EnqueueCommand(new ExitCommand());
		await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
	}

	private void ProcessOnExited(object? sender, EventArgs e)
	{
		_process.ValueOrThrow().Exited -= ProcessOnExited;
		_process = null;
	}

	private (string Command, string Arguments)? GetViceProcessStartupInfo(Settings settings)
	{
		string arguments;
		switch (settings.StartType)
		{
			case ViceStartType.File:
				string? realVicePath = settings.RealVicePath;
				if (!string.IsNullOrWhiteSpace(realVicePath))
				{
					string command = Path.Combine(realVicePath, _osDependent.ViceExeName);
					arguments = _globals.Settings.BinaryMonitorArgument;
					return (command, arguments);
				}
				else
				{
					_dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE",
						"VICE path is not set in settings"));
					return null;
				}
			case ViceStartType.Flatpak:
				arguments = _globals.Settings.BinaryMonitorArgument;
				return ("flatpak", $"run net.sf.VICE {arguments}");
			default:
				return null;
		}
	}

	private Process? StartVice()
	{
		var settings = _globals.Settings;

		var processStartupInfo = GetViceProcessStartupInfo(settings);
		if (processStartupInfo is not null)
		{
			try
			{
				var process = Process.Start(processStartupInfo.Value.Command, processStartupInfo.Value.Arguments);
				process.EnableRaisingEvents = true;
				return process;
			}
			catch (Exception ex)
			{
				_dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Warning, "Starting VICE", ex.Message));
				return null;
			}
		}

		return null;
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
			var checkpointSetResponse = await checkpointSetCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher,
				_logger,
				checkpointSetCommand, ct: ct);
			if (checkpointSetResponse is not null)
			{
				// apply condition to checkpoint if any
				if (!string.IsNullOrWhiteSpace(breakpoint.Condition))
				{
					var conditionSetCommand = _bridge.EnqueueCommand(
						new ConditionSetCommand(checkpointSetResponse.CheckpointNumber, breakpoint.Condition));
					var conditionSetResponse = await conditionSetCommand.Response.AwaitWithLogAndTimeoutAsync(
						_dispatcher, _logger,
						conditionSetCommand, ct: ct);
					// in case condition set fails, remove the checkpoint
					if (conditionSetResponse is null)
					{
						_logger.LogError("Failed setting {Condition}", breakpoint.Condition);
						var checkpointDeleteCommand = _bridge.EnqueueCommand(
							new CheckpointDeleteCommand(checkpointSetResponse.CheckpointNumber),
							resumeOnStopped: true);
						await checkpointDeleteCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger,
							checkpointDeleteCommand, ct: ct);
						return BreakpointError.InvalidCondition;
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

	public async Task<bool> ToggleCheckpointAsync(uint checkpointNumber, bool targetEnabledState,
		CancellationToken ct = default)
	{
		var command = _bridge.EnqueueCommand(new CheckpointToggleCommand(checkpointNumber, targetEnabledState),
			resumeOnStopped: true);
		var result = await command.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger, command, ct: ct);
		return result?.ErrorCode == ErrorCode.OK;
	}

	public async Task<CheckpointListResponse?> GetCheckpointsListAsync(CancellationToken ct = default)
	{
		var checkpointsListCommand = _bridge.EnqueueCommand(new CheckpointListCommand(), resumeOnStopped: true);
		return await checkpointsListCommand.Response.AwaitWithLogAndTimeoutAsync(_dispatcher, _logger,
			checkpointsListCommand, ct: ct);
	}

	protected override void OnPropertyChanged(string? name = null!)
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

	protected override async Task DisposeAsyncCore()
	{
		await _bridgeResponseProcessorCts.CancelAsync();
		await base.DisposeAsyncCore();
	}
}