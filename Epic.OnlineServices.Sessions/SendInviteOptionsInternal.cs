using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct SendInviteOptionsInternal : ISettable<SendInviteOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionName;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public Utf8String SessionName
	{
		set
		{
			Helper.Set(value, ref m_SessionName);
		}
	}

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public ProductUserId TargetUserId
	{
		set
		{
			Helper.Set(value, ref m_TargetUserId);
		}
	}

	public void Set(ref SendInviteOptions other)
	{
		m_ApiVersion = 1;
		SessionName = other.SessionName;
		LocalUserId = other.LocalUserId;
		TargetUserId = other.TargetUserId;
	}

	public void Set(ref SendInviteOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			SessionName = other.Value.SessionName;
			LocalUserId = other.Value.LocalUserId;
			TargetUserId = other.Value.TargetUserId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionName);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
