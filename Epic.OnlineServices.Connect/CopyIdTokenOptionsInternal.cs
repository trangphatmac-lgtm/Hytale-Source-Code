using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct CopyIdTokenOptionsInternal : ISettable<CopyIdTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public void Set(ref CopyIdTokenOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
	}

	public void Set(ref CopyIdTokenOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
