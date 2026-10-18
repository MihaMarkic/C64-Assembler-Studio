using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels.Tools;

namespace C64AssemblerStudio.Engine.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProfilerViewModel: NotifiableObject, IToolView
{
	public string Header => "Profiler";
	private readonly IUiServices _uiServices;
	public bool IsCollecting { get; private set; }
	/// <summary>
	/// The amount of data collected so far, expressed in bytes. 
	/// </summary>
	public int CollectedData { get; private set; }
	private IProfilerStorage? _profilerStorage = null;

	public ProfilerViewModel(IUiServices uiServices)
	{
		_uiServices = uiServices;
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

	public void StopProfiling()
	{
		_profilerStorage.ValueOrThrow().CollectedData -= ProfilerStorageOnCollectedData;
		_profilerStorage = null;
		IsCollecting = false;
	}
}