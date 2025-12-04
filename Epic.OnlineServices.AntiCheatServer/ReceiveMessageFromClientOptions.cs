using System;

namespace Epic.OnlineServices.AntiCheatServer;

public struct ReceiveMessageFromClientOptions
{
	public IntPtr ClientHandle { get; set; }

	public ArraySegment<byte> Data { get; set; }
}
