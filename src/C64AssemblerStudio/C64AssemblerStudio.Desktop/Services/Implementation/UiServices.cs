using C64AssemblerStudio.Core.Services.Abstract;

namespace C64AssemblerStudio.Desktop.Services.Implementation;

/// <inheritdoc/>
public class UiServices : IUiServices
{
	private readonly TaskScheduler _taskUiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

	/// <inheritdoc/>
	public Task ExecuteAsync<T>(Action<T> action, T data,
		TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, CancellationToken ct = default)
	{
		return Task.Factory.StartNew(d =>
		{
			var source = (T)d!;
			action(source);
		}, data, CancellationToken.None, TaskCreationOptions.None, _taskUiScheduler);
	}

	/// <inheritdoc/>
	public Task ExecuteAsync<T>(Func<T, Task> func, T data,
		TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, CancellationToken ct = default)
	{
		return Task.Factory.StartNew(async d =>
		{
			var source = (T)d!;
			await func(source);
		}, data, CancellationToken.None, TaskCreationOptions.None, _taskUiScheduler);
	}

	/// <inheritdoc/>
	public Task<TResult> ExecuteAsync<TData, TResult>(Func<TData, Task<TResult>> func, TData data,
		TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, CancellationToken ct = default)
	{
		return Task.Factory.StartNew(async d =>
			{
				var source = (TData)d!;
				return await func(source);
			}, data, CancellationToken.None, TaskCreationOptions.None, _taskUiScheduler)
			.Unwrap();
	}
}