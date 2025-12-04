using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct RejectInviteOptionsInternal : ISettable<RejectInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_InviteId;

	private IntPtr m_LocalUserId;

	public Utf8String InviteId
	{
		set
		{
			Helper.Set(value, ref m_InviteId);
		}
	}

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public void Set(ref RejectInviteOptions other)
	{
		m_ApiVersion = 1;
		InviteId = other.InviteId;
		LocalUserId = other.LocalUserId;
	}

	public void Set(ref RejectInviteOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			InviteId = other.Value.InviteId;
			LocalUserId = other.Value.LocalUserId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_InviteId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
