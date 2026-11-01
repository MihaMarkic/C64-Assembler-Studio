using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.Profiler;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Righthand.ViceMonitor.Bridge.Shared;

namespace C64AssemblerStudio.Engine.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProfilerViewModel : NotifiableObject, IToolView
{
	public string Header => "Profiler";
	private readonly IUiServices _uiServices;
	public DebugOutputViewModel DebugOutput { get; }
	public RegistersViewModel Registers { get; }
	public bool IsCollecting { get; private set; }
	/// <summary>
	/// The amount of data collected so far, expressed in bytes. 
	/// </summary>
	public int CollectedData { get; private set; }

	public string? ErrorText { get; private set; }
	private IProfilerStorage? _profilerStorage = null;

	public ProfilerViewModel(IUiServices uiServices, DebugOutputViewModel debugOutputViewModel,
		RegistersViewModel registersViewModel)
	{
		_uiServices = uiServices;
		DebugOutput = debugOutputViewModel;
		Registers = registersViewModel;
	}

	public void Init(IProfilerStorage profilerStorage)
	{
		_profilerStorage = profilerStorage;
		_profilerStorage.CollectedData += ProfilerStorageOnCollectedData;
		CollectedData = 0;
		IsCollecting = true;
	}

	private void ProfilerStorageOnCollectedData(object? sender, ProfilerCollectedDataEventArgs e)
	{
		// event is from an unknown thread, execute on UI thread
		_uiServices.ExecuteAsync(UpdateCounters, e);
	}

	private void UpdateCounters(ProfilerCollectedDataEventArgs e)
	{
		CollectedData += e.Segment;
	}

	private CancellationTokenSource? _analyseProfilerDataCts;

	public async Task StopProfilingAsync()
	{
		await _analyseProfilerDataCts.CancelNullableAsync();
		_profilerStorage.ValueOrThrow().CollectedData -= ProfilerStorageOnCollectedData;
		IsCollecting = false;
		_analyseProfilerDataCts = new();
		try
		{
			await AnalyseProfileDataAsync(_analyseProfilerDataCts.Token);
		}
		catch (Exception e)
		{
			DebugOutput.AddLine($"Analyse profiler data failed: {e.Message}");
			ErrorText = e.Message;
		}

		_profilerStorage = null;
	}

	public async Task AnalyseProfileDataAsync(CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(_profilerStorage);
		if (Registers.PcRegisterId is null)
		{
			throw new Exception("Could not determine PC register index");
		}

		var instructions = _profilerStorage.GetHistoryItemsChunksAsync(ct).SelectMany(i => i)
			.GetAsyncEnumerator();
		var application = await CreateApplicationAsync(instructions, ct);
	}

	public async Task<ProfiledApplication> CreateApplicationAsync(IAsyncEnumerator<CpuHistoryItem> app,
		CancellationToken ct)
	{
		// number of different interrupt methods should be really low, no need to use HashSet here
		var interruptMethods = new List<ProfiledMethod>();
		var entryMethod = await CreateMethodAsync(app, interruptMethods, ct);
		if (entryMethod is null)
		{
			throw new Exception("Couldn't analyse entry method");
		}

		return new ProfiledApplication(entryMethod, [.. interruptMethods]);
	}

	public async Task<ProfiledMethod?> CreateMethodAsync(IAsyncEnumerator<CpuHistoryItem> app,
		IList<ProfiledMethod> interruptMethods, CancellationToken ct)
	{
		if (!(await app.MoveNextAsync()))
		{
			return null;
		}

		var entryInstruction = app.Current;
		var entryAddress = entryInstruction.GetAddress(Registers.PcRegisterId!.Value)
			.ValueOrThrow("Couldn't determine entry method address");

		return new(entryAddress, [], []);
	}
}