namespace System.Threading.Tasks;

public static class TaskExtension
{
    public static async Task<T> AwaitWithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken ct = default)
    {
        bool success = await Task.WhenAny(task, Task.Delay(timeout, ct)) == task;
        if (!success)
        {
            throw new TimeoutException();
        }
        return task.Result;
    }

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
        return factory.StartNew(o => function((TArg?)o), state, ct);
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
