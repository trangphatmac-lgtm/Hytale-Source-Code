using System;

namespace Epic.OnlineServices.AntiCheatServer;

public struct SetClientNetworkStateOptions
{
	public IntPtr ClientHandle { get; set; }

	public bool IsNetworkActive { get; set; }
}
