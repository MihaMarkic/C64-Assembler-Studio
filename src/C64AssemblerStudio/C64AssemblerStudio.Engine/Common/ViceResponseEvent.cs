using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public abstract class ViceResponseEvent<T> : EventArgs
    where T : ViceResponse
{
    public T? Response { get; }

    protected ViceResponseEvent(T? response)
    {
        Response = response;
    }
    
}