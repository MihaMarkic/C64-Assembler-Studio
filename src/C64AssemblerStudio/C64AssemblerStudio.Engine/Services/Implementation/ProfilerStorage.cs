using System.Threading.Tasks.Dataflow;
using C64AssemblerStudio.Engine.Models.Profiler;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Services.Implementation;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ProfilerStorage : IProfilerStorage
{
	public event EventHandler<ProfilerCollectedDataEventArgs>? CollectedData;
	public event EventHandler<ProfilerStorageErrorEventArgs>? Error;
	
	private readonly ILogger<ProfilerStorage> _logger;
	private readonly IFileService _fileService;
	private readonly string _profilerPath;
	private readonly BufferBlock<CpuHistoryBufferResponse> _buffer = new BufferBlock<CpuHistoryBufferResponse>();
	private CancellationTokenSource? _cts = new CancellationTokenSource();
	private Task? _loop;
	
	public ProfilerStorage(string projectPath,  ILogger<ProfilerStorage> logger,  IFileService fileService)
	{
		_logger = logger;
		_fileService = fileService;
		var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		_profilerPath = Path.Combine(userDirectory, "_profiler"); // Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
		// _profilerPath = Path.Combine(projectPath, "_profiler");
	}
	
	private void OnError(ProfilerStorageErrorEventArgs e) => Error?.Invoke(this, e);
	private void OnCollectedData(ProfilerCollectedDataEventArgs e) => CollectedData?.Invoke(this, e);

	public Task InitAsync(CancellationToken ct)
	{
		if (_cts is null)
		{
			_logger.LogError("Can't reuse");
			throw new Exception("Can't reuse");
		}
		_logger.LogDebug("Initializing");
		_fileService.CreateDirectory(_profilerPath);
		foreach (var file in _fileService.GetFiles(_profilerPath, "*.*"))
		{
			_fileService.DeleteFile(file);
		}

		var token = _cts!.Token;
		var task = Task.Factory.StartNew(
			() => StartAsync(_buffer, token), 
			cancellationToken: token, 
			creationOptions: TaskCreationOptions.LongRunning,
			scheduler: TaskScheduler.Default);
		_loop = task.Unwrap();
		_logger.LogDebug("Initialized");
		return Task.CompletedTask;
	}

	private async Task StartAsync(IReceivableSourceBlock<CpuHistoryBufferResponse> source, CancellationToken ct)
	{
		int currentIndex = 0;
		while (await source.OutputAvailableAsync(ct).ConfigureAwait(false))
		{
			try
			{
				if (source.TryReceive(out var response))
				{
					try
					{
						string filePath = GetChunkFilePath(currentIndex);
						var data = response.Buffer!.Value.Data;
						currentIndex++;
						await _fileService.WriteAllBytesAsync(filePath, data.AsMemory(), ct)
							.ConfigureAwait(false);
						OnCollectedData(new ProfilerCollectedDataEventArgs(data.Length));
					}
					finally
					{
						response.Dispose();
					}
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogDebug("Exiting loop because cancelled");
			}
		}
		_logger.LogDebug("Loop done");
	}

	public string GetChunkFilePath(int index) => Path.Combine(_profilerPath, $"chunk{index}.prd"); 
	
	public Task<bool> StoreChunkAsync(CpuHistoryBufferResponse response)
	{
		if (_buffer.Completion.IsCompleted)
		{
			_logger.LogWarning("Loop already completed");
			return Task.FromResult(false);
		}
		return _buffer.SendAsync(response);
	}

	public async Task FlushAsync(CancellationToken ct)
	{
		if (_loop is null)
		{
			_logger.LogWarning("Can't flush, loop is already done");
			return;
		}
		_logger.LogDebug("Marking buffer as complete");
		_buffer.Complete();
		await _loop;
		_cts?.Dispose();
		_cts = null;
		_loop = null;
		_logger.LogDebug("Flush complete");
	}
	public async Task CancelAsync(CancellationToken ct)
	{
		if (_cts is not null && _loop is not null)
		{
			_logger.LogDebug("Dispose async");
			await _cts.CancelAsync();
			_cts.Dispose();
			_cts = null;
			try
			{
				await _loop;
			}
			catch (OperationCanceledException)
			{
			}

			_logger.LogDebug("Disposed async");
			_loop = null;
		}
		else
		{
			_logger.LogDebug("Nothing to dispose async");
		}
	}

	public async Task<ProfilerStorageInfo> GetInfoAsync(CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}