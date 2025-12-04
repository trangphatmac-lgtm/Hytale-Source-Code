using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct UpdateReceivingCallbackInfoInternal : ICallbackInfoInternal, IGettable<UpdateReceivingCallbackInfo>, ISettable<UpdateReceivingCallbackInfo>, IDisposable
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

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

	public ProductUserId ParticipantId
	{
		get
		{
			Helper.Get(m_ParticipantId, out ProductUserId to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ParticipantId);
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

	public void Set(ref UpdateReceivingCallbackInfo other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		ParticipantId = other.ParticipantId;
		DataEnabled = other.DataEnabled;
	}

	public void Set(ref UpdateReceivingCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ResultCode = other.Value.ResultCode;
			ClientData = other.Value.ClientData;
			LocalUserId = other.Value.LocalUserId;
			RoomName = other.Value.RoomName;
			ParticipantId = other.Value.ParticipantId;
			DataEnabled = other.Value.DataEnabled;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ParticipantId);
	}

	public void Get(out UpdateReceivingCallbackInfo output)
	{
		output = default(UpdateReceivingCallbackInfo);
		output.Set(ref this);
	}
}
