using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct UpdateSendingCallbackInfoInternal : ICallbackInfoInternal, IGettable<UpdateSendingCallbackInfo>, ISettable<UpdateSendingCallbackInfo>, IDisposable
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private int m_DataEnabled;

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

	public Utf8String RoomName
	{
		get
		{
			Helper.Get(m_RoomName, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_RoomName);
		}
	}

	public bool DataEnabled
	{
		get
		{
			Helper.Get(m_DataEnabled, out var to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_DataEnabled);
		}
	}

	public void Set(ref UpdateSendingCallbackInfo other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		DataEnabled = other.DataEnabled;
	}

	public void Set(ref UpdateSendingCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ResultCode = other.Value.ResultCode;
			ClientData = other.Value.ClientData;
			LocalUserId = other.Value.LocalUserId;
			RoomName = other.Value.RoomName;
			DataEnabled = other.Value.DataEnabled;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
	}

	public void Get(out UpdateSendingCallbackInfo output)
	{
		output = default(UpdateSendingCallbackInfo);
		output.Set(ref this);
	}
}
