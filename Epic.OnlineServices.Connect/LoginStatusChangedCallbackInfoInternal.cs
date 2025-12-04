using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct LoginStatusChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LoginStatusChangedCallbackInfo>, ISettable<LoginStatusChangedCallbackInfo>, IDisposable
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private LoginStatus m_PreviousStatus;

	private LoginStatus m_CurrentStatus;

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

	public ProductUserId LocalUserId
	{
		get
		{
			Helper.Get(m_LocalUserId, out ProductUserId to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public LoginStatus PreviousStatus
	{
		get
		{
			return m_PreviousStatus;
		}
		set
		{
			m_PreviousStatus = value;
		}
	}

	public LoginStatus CurrentStatus
	{
		get
		{
			return m_CurrentStatus;
		}
		set
		{
			m_CurrentStatus = value;
		}
	}

	public void Set(ref LoginStatusChangedCallbackInfo other)
	{
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		PreviousStatus = other.PreviousStatus;
		CurrentStatus = other.CurrentStatus;
	}

	public void Set(ref LoginStatusChangedCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ClientData = other.Value.ClientData;
			LocalUserId = other.Value.LocalUserId;
			PreviousStatus = other.Value.PreviousStatus;
			CurrentStatus = other.Value.CurrentStatus;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_LocalUserId);
	}

	public void Get(out LoginStatusChangedCallbackInfo output)
	{
		output = default(LoginStatusChangedCallbackInfo);
		output.Set(ref this);
	}
}
