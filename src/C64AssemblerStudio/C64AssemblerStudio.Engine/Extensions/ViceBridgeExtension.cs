using Microsoft.Extensions.Logging;
using C64AssemblerStudio.Engine.Messages;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;

namespace Righthand.ViceMonitor.Bridge.Services.Abstract;

public static class ViceBridgeExtension
{
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    public static async Task<TResponse?> AwaitWithLogAndTimeoutAsync<TCommand, TResponse>(
        this Task<CommandResponse<TResponse>> task, IDispatcher dispatcher, ILogger logger, 
        TCommand command, Func<TResponse, Task> action, 
        TimeSpan? timeout = null, CancellationToken ct = default)
        where TCommand: ViceCommand<TResponse>
        where TResponse : ViceResponse
    {
	    try
        {
            var cr = await command.Response.AwaitWithTimeoutAsync(timeout ?? DefaultTimeout, ct);
            if (LogResponseErrors<TCommand, TResponse>(dispatcher, logger, cr))
            {
                var response = cr.Response!;
                {
                    await action(response);
                }
                return response;
            }
            return null;
        }
        catch (TimeoutException)
        {
            LogTimeout<TCommand>(dispatcher, logger);
            return null;
        }
    }
    static bool LogResponseErrors<TCommand, TResponse>(IDispatcher dispatcher, ILogger logger, CommandResponse<TResponse> cr)
        where TCommand: IViceCommand
        where TResponse : ViceResponse
    {
        if (!cr.IsSuccess)
        {
            logger.LogError("Response for command {Command} returned error code {ErrorCode}", typeof(TCommand).Name, cr.ErrorCode);
            dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Communication", 
                $"Response for command {typeof(TCommand).Name} returned error code {cr.ErrorCode}"));
            return false;
        }
        return true;
    }
    public static async Task<TResponse?> AwaitWithLogAndTimeoutAsync<TCommand, TResponse>(
        this Task<CommandResponse<TResponse>> task, IDispatcher dispatcher, ILogger logger, TCommand command, 
        Action<TResponse> action,
        TimeSpan? timeout = default, CancellationToken ct = default)
        where TCommand : ViceCommand<TResponse>
        where TResponse : ViceResponse
    {
	    try
        {
            var cr = await command.Response.AwaitWithTimeoutAsync(timeout ?? DefaultTimeout, ct);
            if (LogResponseErrors<TCommand, TResponse>(dispatcher, logger, cr))
            {
                var response = cr.Response!;
                {
                    action(response);
                }
                return response;
            }
            return default;
        }
        catch (TimeoutException)
        {
            LogTimeout<TCommand>(dispatcher, logger);
            return null;
        }
    }

    public static async Task<TResponse?> AwaitWithLogAndTimeoutAsync<TCommand, TResponse>(
	    this Task<CommandResponse<TResponse>> task, IDispatcher dispatcher, ILogger logger, TCommand command,
        TimeSpan? timeout = null, CancellationToken ct = default)
        where TCommand: ViceCommand<TResponse>
        where TResponse: ViceResponse
    {
        try
        {
            var cr = await task.AwaitWithTimeoutAsync(timeout ?? DefaultTimeout, ct);
            if (LogResponseErrors<TCommand, TResponse>(dispatcher, logger, cr))
            {
                return cr.Response!;
            }
            return null;
        }
        catch (TimeoutException)
        {
            LogTimeout<TCommand>(dispatcher, logger);
            return null;
        }
    }

    internal static void LogTimeout<TCommand>(IDispatcher dispatcher, ILogger logger)
    {
        logger.LogError("Timeout occurred while executing {Command}", typeof(TCommand).Name);
        dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Communication", $"Timeout occurred while executing {typeof(TCommand).Name}"));
    }
    
    /// <inheritdoc/>
    // internal static async Task WaitForStoppedAsync(CancellationToken ct)
    // {
	   //  await WaitForUnboundResponseAsync<StoppedResponse>(r => true, waitForStop: false, ct);
    // }

    /// <summary>
    /// Waits for an unbound response of given type and condition <paramref name="isResponseValid"/>.
    /// When <paramref name="waitForStop"/> is true, it waits for <see cref="StoppedResponse"/> as well.
    /// </summary>
    /// <param name="bridge"></param>
    /// <param name="logger"></param>
    /// <param name="isResponseValid"></param>
    /// <param name="waitForStop"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static Task<T> WaitForUnboundResponseAsync<T>(this IViceBridge bridge, ILogger logger, Func<T, bool> isResponseValid, bool waitForStop,
	    CancellationToken ct)
	    where T : ViceResponse
    {
	    TaskCompletionSource<T> tcs = new();
	    T? result = null;
	    EventHandler<ViceResponseEventArgs>? handler = null;
	    handler = (_, e) =>
	    {
		    logger.LogDebug("While waiting got reponse {ResponseType}", e.Response.GetType().Name);
		    if (!waitForStop || result is null)
		    {
			    logger.LogDebug("Checking for result response");
			    if (e.Response is T response && isResponseValid(response))
			    {
				    if (waitForStop)
				    {
					    logger.LogDebug("Got response but will wait for Stopped now");
					    result = response;
				    }
				    else
				    {
					    tcs.TrySetResult(response);
					    bridge.ViceResponse -= handler;
				    }
			    }
		    }
		    // here the result is set
		    else if (waitForStop)
		    {
			    logger.LogDebug("Checking for stopped response");
			    if (e.Response is StoppedResponse)
			    {
				    tcs.TrySetResult(result);
				    bridge.ViceResponse -= handler;
			    }
		    }
	    };
	    logger.LogDebug("Waiting for reponse {ResponseType}", typeof(T).Name);
	    bridge.ViceResponse += handler;
	    ct.Register(() => { bridge.ViceResponse -= handler; });
	    return tcs.Task;
    }
}
