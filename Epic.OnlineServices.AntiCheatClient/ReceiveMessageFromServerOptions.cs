using System;

namespace Epic.OnlineServices.AntiCheatClient;

public struct ReceiveMessageFromServerOptions
{
	public ArraySegment<byte> Data { get; set; }
}
