using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public class CheckpointInfoEventArgs: ViceResponseEvent<CheckpointInfoResponse>
{
    public CheckpointInfoEventArgs(CheckpointInfoResponse? response) : base(response)
    {
    }
}