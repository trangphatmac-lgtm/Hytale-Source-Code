using System;

namespace Epic.OnlineServices.AntiCheatServer;

public struct ProtectMessageOptions
{
	public IntPtr ClientHandle { get; set; }

	public ArraySegment<byte> Data { get; set; }

	public uint OutBufferSizeBytes { get; set; }
}
