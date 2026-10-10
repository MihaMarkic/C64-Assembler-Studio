using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public class CpuHistoryEventArgs : ViceResponseEvent<CpuHistoryBufferResponse>
{
	public CpuHistoryEventArgs(CpuHistoryBufferResponse? response) : base(response)
	{
	}
}