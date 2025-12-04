using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ActiveSessionGetRegisteredPlayerCountOptionsInternal : ISettable<ActiveSessionGetRegisteredPlayerCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref ActiveSessionGetRegisteredPlayerCountOptions other)
	{
		m_ApiVersion = 1;
	}

	public void Set(ref ActiveSessionGetRegisteredPlayerCountOptions? other)
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
