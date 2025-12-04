using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.AntiCheatServer;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct AddNotifyClientAuthStatusChangedOptionsInternal : ISettable<AddNotifyClientAuthStatusChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyClientAuthStatusChangedOptions other)
	{
		m_ApiVersion = 1;
	}

	public void Set(ref AddNotifyClientAuthStatusChangedOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
		}
	}

	public void Dispose()
	{
	}
}
