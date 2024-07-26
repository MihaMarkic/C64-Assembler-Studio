using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public class RegistersEventArgs: EventArgs
{
    public RegistersResponse Response { get; }

    public RegistersEventArgs(RegistersResponse response)
    {
        Response = response;
    }
}