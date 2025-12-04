using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct RejectInviteOptionsInternal : ISettable<RejectInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_InviteId;

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public Utf8String InviteId
	{
		set
		{
			Helper.Set(value, ref m_InviteId);
		}
	}

	public void Set(ref RejectInviteOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
		InviteId = other.InviteId;
	}

	public void Set(ref RejectInviteOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
			InviteId = other.Value.InviteId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_InviteId);
	}
}
