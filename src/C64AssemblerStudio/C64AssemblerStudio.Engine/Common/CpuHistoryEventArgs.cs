using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Common;

public class CpuHistoryEventArgs : ViceResponseEvent<CpuHistoryResponse>
{
	public CpuHistoryEventArgs(CpuHistoryResponse? response) : base(response)
	{
	}
}