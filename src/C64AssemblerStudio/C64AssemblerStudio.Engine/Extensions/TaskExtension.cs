namespace System.Threading.Tasks;

/// <summary>
/// Provides <see cref="Task"/> extensions.
/// </summary>
public static class TaskExtension
{
    /// <summary>
    /// Awaits task, timeout or token cancellation. If timeout is triggered, a <see cref="TimeoutException"/> is thrown,
    /// if <paramref name="ct"/> signals cancellation, a <see cref="TaskCanceledException"/> is throw, otherwise no exception 
    /// is thrown by this method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <param name="timeout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="TaskCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    public static async Task<T> AwaitWithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken ct = default)
    {
        bool success = await Task.WhenAny(task, Task.Delay(timeout, ct)) == task;
        if (!success)
        {
            if (ct.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            else
            {
                throw new TimeoutException();
            }
        }
        return task.Result;
    }
    /// <summary>
    /// Allows invoking cancellation on null <paramref name="cts"/> sources.
    /// </summary>
    /// <param name="cts"></param>
    /// <param name="ct"></param>
    /// <returns>An instance of <see cref="Task"/>.</returns>
    /// <remarks>This method is useful when dealing with nullable <see cref="CancellationTokenSource"/>.</remarks>
    public static Task CancelNullableAsync(this CancellationTokenSource? cts, CancellationToken ct = default)
    {
        if (cts is null)
        {
            return Task.CompletedTask;
        }
        else
        {
            return cts.CancelAsync();
        }
    }

    public static Task StartNewTyped<TArg>(this TaskFactory factory, Func<TArg?, Task> function,
        TArg state,
        CancellationToken ct)
    {
        return factory.StartNew(async o => await function((TArg?)o), state, ct);
    }
    public static Task StartNewTyped<TArg>(this TaskFactory factory, Action<TArg?> action,
        TArg state,
        CancellationToken ct)
    {
        return factory.StartNew(o => action((TArg?)o), state, ct);
    }
    public static Task StartNewTyped<TResult, TArg>(this TaskFactory factory, Func<TArg?, Task<TResult>> function,
        TArg state,
        CancellationToken ct)
    {
        return factory.StartNew(o => function((TArg?)o), state, ct);
    }
}
