using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct ParticipantUpdatedCallbackInfoInternal : ICallbackInfoInternal, IGettable<ParticipantUpdatedCallbackInfo>, ISettable<ParticipantUpdatedCallbackInfo>, IDisposable
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private RTCDataStatus m_DataStatus;

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

	public RTCDataStatus DataStatus
	{
		get
		{
			return m_DataStatus;
		}
		set
		{
			m_DataStatus = value;
		}
	}

	public void Set(ref ParticipantUpdatedCallbackInfo other)
	{
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		ParticipantId = other.ParticipantId;
		DataStatus = other.DataStatus;
	}

	public void Set(ref ParticipantUpdatedCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ClientData = other.Value.ClientData;
			LocalUserId = other.Value.LocalUserId;
			RoomName = other.Value.RoomName;
			ParticipantId = other.Value.ParticipantId;
			DataStatus = other.Value.DataStatus;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ParticipantId);
	}

	public void Get(out ParticipantUpdatedCallbackInfo output)
	{
		output = default(ParticipantUpdatedCallbackInfo);
		output.Set(ref this);
	}
}
