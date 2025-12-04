using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct RejectInviteCallbackInfoInternal : ICallbackInfoInternal, IGettable<RejectInviteCallbackInfo>, ISettable<RejectInviteCallbackInfo>, IDisposable
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

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

	public void Set(ref RejectInviteCallbackInfo other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
	}

	public void Set(ref RejectInviteCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ResultCode = other.Value.ResultCode;
			ClientData = other.Value.ClientData;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
	}

	public void Get(out RejectInviteCallbackInfo output)
	{
		output = default(RejectInviteCallbackInfo);
		output.Set(ref this);
	}
}
