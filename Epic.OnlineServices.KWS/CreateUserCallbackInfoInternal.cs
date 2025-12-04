using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.KWS;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct CreateUserCallbackInfoInternal : ICallbackInfoInternal, IGettable<CreateUserCallbackInfo>, ISettable<CreateUserCallbackInfo>, IDisposable
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_KWSUserId;

	private int m_IsMinor;

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

	public Utf8String KWSUserId
	{
		get
		{
			Helper.Get(m_KWSUserId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_KWSUserId);
		}
	}

	public bool IsMinor
	{
		get
		{
			Helper.Get(m_IsMinor, out var to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_IsMinor);
		}
	}

	public void Set(ref CreateUserCallbackInfo other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		KWSUserId = other.KWSUserId;
		IsMinor = other.IsMinor;
	}

	public void Set(ref CreateUserCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ResultCode = other.Value.ResultCode;
			ClientData = other.Value.ClientData;
			LocalUserId = other.Value.LocalUserId;
			KWSUserId = other.Value.KWSUserId;
			IsMinor = other.Value.IsMinor;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_KWSUserId);
	}

	public void Get(out CreateUserCallbackInfo output)
	{
		output = default(CreateUserCallbackInfo);
		output.Set(ref this);
	}
}
