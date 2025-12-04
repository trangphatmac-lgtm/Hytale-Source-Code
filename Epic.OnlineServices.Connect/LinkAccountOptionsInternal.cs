using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct LinkAccountOptionsInternal : ISettable<LinkAccountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ContinuanceToken;

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public ContinuanceToken ContinuanceToken
	{
		set
		{
			Helper.Set(value, ref m_ContinuanceToken);
		}
	}

	public void Set(ref LinkAccountOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
		ContinuanceToken = other.ContinuanceToken;
	}

	public void Set(ref LinkAccountOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
			ContinuanceToken = other.Value.ContinuanceToken;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ContinuanceToken);
	}
}
