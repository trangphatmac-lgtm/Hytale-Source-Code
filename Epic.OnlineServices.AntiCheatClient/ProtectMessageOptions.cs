using System;

namespace Epic.OnlineServices.AntiCheatClient;

public struct ProtectMessageOptions
{
	public ArraySegment<byte> Data { get; set; }

	public uint OutBufferSizeBytes { get; set; }
}
