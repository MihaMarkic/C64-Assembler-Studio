using C64AssemblerStudio.Engine.Models.Profiler;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public interface IProfilerStorage
{
	event EventHandler<ProfilerCollectedDataEventArgs>? CollectedData;
	event EventHandler<ProfilerStorageErrorEventArgs>? Error;
	Task InitAsync(CancellationToken ct);
	Task<bool> StoreChunkAsync(CpuHistoryBufferResponse response);
	Task FlushAsync(CancellationToken ct);
	Task<ProfilerStorageInfo> GetInfoAsync(CancellationToken ct);
	string GetChunkFilePath(int index);
}

public class ProfilerCollectedDataEventArgs : EventArgs
{
	public int Segment { get; }

	public ProfilerCollectedDataEventArgs(int segment)
	{
		Segment = segment;
	}
}