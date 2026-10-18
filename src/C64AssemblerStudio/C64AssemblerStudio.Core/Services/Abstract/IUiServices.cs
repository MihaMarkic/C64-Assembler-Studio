namespace C64AssemblerStudio.Core.Services.Abstract;

/// <summary>
/// Executes code on UI thread.
/// </summary>
public interface IUiServices
{
	/// <summary>
	/// Executes given lambda with strong typed parameter.
	/// </summary>
	/// <param name="action"></param>
	/// <param name="taskCreationOptions">Default is <see cref="TaskCreationOptions.None"/>.</param>
	/// <param name="ct"></param>
	/// <param name="data"></param>
	/// <returns></returns>
	Task ExecuteAsync<T>(Action<T> action, T data, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None,
		CancellationToken ct = default);
	/// <summary>
	/// Executes given lambda with strong typed parameter and awaits for it.
	/// </summary>
	/// <param name="func"></param>
	/// <param name="data"></param>
	/// <param name="taskCreationOptions"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task ExecuteAsync<T>(Func<T, Task> func, T data,
		TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, CancellationToken ct = default);

	/// <summary>
	/// Executes given lambda with strong typed parameter and awaits for result.
	/// </summary>
	/// <param name="func"></param>
	/// <param name="data"></param>
	/// <param name="taskCreationOptions"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<TResult> ExecuteAsync<TData, TResult>(Func<TData, Task<TResult>> func, TData data,
		TaskCreationOptions taskCreationOptions = TaskCreationOptions.None, CancellationToken ct = default);
}