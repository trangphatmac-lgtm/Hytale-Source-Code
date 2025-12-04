using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct EndSessionOptionsInternal : ISettable<EndSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	public Utf8String SessionName
	{
		set
		{
			Helper.Set(value, ref m_SessionName);
		}
	}

	public void Set(ref EndSessionOptions other)
	{
		m_ApiVersion = 1;
		SessionName = other.SessionName;
	}

	public void Set(ref EndSessionOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			SessionName = other.Value.SessionName;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
	}
}
