using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public class RegistersEventArgs: ViceResponseEvent<RegistersResponse>
{
    public RegistersEventArgs(RegistersResponse? response) : base(response)
    {
    }
}