using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.AntiCheatServer;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ProtectMessageOptionsInternal : ISettable<ProtectMessageOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private uint m_DataLengthBytes;

	private IntPtr m_Data;

	private uint m_OutBufferSizeBytes;

	public IntPtr ClientHandle
	{
		set
		{
			m_ClientHandle = value;
		}
	}

	public ArraySegment<byte> Data
	{
		set
		{
			Helper.Set(value, ref m_Data, out m_DataLengthBytes);
		}
	}

	public uint OutBufferSizeBytes
	{
		set
		{
			m_OutBufferSizeBytes = value;
		}
	}

	public void Set(ref ProtectMessageOptions other)
	{
		m_ApiVersion = 1;
		ClientHandle = other.ClientHandle;
		Data = other.Data;
		OutBufferSizeBytes = other.OutBufferSizeBytes;
	}

	public void Set(ref ProtectMessageOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			ClientHandle = other.Value.ClientHandle;
			Data = other.Value.Data;
			OutBufferSizeBytes = other.Value.OutBufferSizeBytes;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientHandle);
		Helper.Dispose(ref m_Data);
	}
}
