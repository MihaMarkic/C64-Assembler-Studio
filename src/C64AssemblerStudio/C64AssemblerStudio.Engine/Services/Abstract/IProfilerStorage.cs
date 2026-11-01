using C64AssemblerStudio.Engine.Models.Profiler;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Shared;

namespace C64AssemblerStudio.Engine.Services.Implementation;

/// <summary>
/// Provides storage for Profiler data.
/// </summary>
public interface IProfilerStorage
{
	event EventHandler<ProfilerCollectedDataEventArgs>? CollectedData;
	event EventHandler<ProfilerStorageErrorEventArgs>? Error;
	Task InitAsync(CancellationToken ct);
	Task<bool> StoreChunkAsync(CpuHistoryBufferResponse response);
	Task FlushAsync(CancellationToken ct);
	Task<ProfilerStorageInfo> GetInfoAsync(CancellationToken ct);

	/// <summary>
	/// Provides an iterator over all chunks.
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	/// <remarks>Loads next chunk in background.</remarks>
	IAsyncEnumerable<ImmutableArray<CpuHistoryItem>> GetHistoryItemsChunksAsync(CancellationToken ct);
}

public class ProfilerCollectedDataEventArgs : EventArgs
{
	public int Segment { get; }

	public ProfilerCollectedDataEventArgs(int segment)
	{
		Segment = segment;
	}
}