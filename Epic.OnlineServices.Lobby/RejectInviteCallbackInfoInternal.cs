using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct RejectInviteCallbackInfoInternal : ICallbackInfoInternal, IGettable<RejectInviteCallbackInfo>, ISettable<RejectInviteCallbackInfo>, IDisposable
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_InviteId;

	public Result ResultCode
	{
		get
		{
			return m_ResultCode;
		}
		set
		{
			m_ResultCode = value;
		}
	}

	public object ClientData
	{
		get
		{
			Helper.Get(m_ClientData, out object to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ClientData);
		}
	}

	public IntPtr ClientDataAddress => m_ClientData;

	public Utf8String InviteId
	{
		get
		{
			Helper.Get(m_InviteId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_InviteId);
		}
	}

	public void Set(ref RejectInviteCallbackInfo other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		InviteId = other.InviteId;
	}

	public void Set(ref RejectInviteCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ResultCode = other.Value.ResultCode;
			ClientData = other.Value.ClientData;
			InviteId = other.Value.InviteId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_InviteId);
	}

	public void Get(out RejectInviteCallbackInfo output)
	{
		output = default(RejectInviteCallbackInfo);
		output.Set(ref this);
	}
}
