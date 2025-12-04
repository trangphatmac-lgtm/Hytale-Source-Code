using System;

namespace Epic.OnlineServices.UI;

public struct MemoryMonitorCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public IntPtr SystemMemoryMonitorReport { get; set; }

	public Result? GetResultCode()
	{
		return null;
	}

	internal void Set(ref MemoryMonitorCallbackInfoInternal other)
	{
		ClientData = other.ClientData;
		SystemMemoryMonitorReport = other.SystemMemoryMonitorReport;
	}
}
