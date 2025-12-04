using System;

namespace Epic.OnlineServices.AntiCheatClient;

public struct ReceiveMessageFromPeerOptions
{
	public IntPtr PeerHandle { get; set; }

	public ArraySegment<byte> Data { get; set; }
}
