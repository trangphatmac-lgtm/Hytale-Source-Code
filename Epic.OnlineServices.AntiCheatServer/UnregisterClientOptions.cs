using System;

namespace Epic.OnlineServices.AntiCheatServer;

public struct UnregisterClientOptions
{
	public IntPtr ClientHandle { get; set; }
}
