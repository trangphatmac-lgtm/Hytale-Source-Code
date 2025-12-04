using System;

namespace Epic.OnlineServices.AntiCheatClient;

public struct UnprotectMessageOptions
{
	public ArraySegment<byte> Data { get; set; }

	public uint OutBufferSizeBytes { get; set; }
}
