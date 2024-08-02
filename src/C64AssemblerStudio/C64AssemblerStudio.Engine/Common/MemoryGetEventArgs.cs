using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public class MemoryGetEventArgs : ViceResponseEvent<MemoryGetResponse>
{
    public MemoryGetEventArgs(MemoryGetResponse? response) : base(response)
    {
    }
}