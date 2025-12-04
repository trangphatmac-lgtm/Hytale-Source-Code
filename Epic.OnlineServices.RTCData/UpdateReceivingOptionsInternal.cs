using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct UpdateReceivingOptionsInternal : ISettable<UpdateReceivingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_ParticipantId;

	private int m_DataEnabled;

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public Utf8String RoomName
	{
		set
		{
			Helper.Set(value, ref m_RoomName);
		}
	}

	public ProductUserId ParticipantId
	{
		set
		{
			Helper.Set(value, ref m_ParticipantId);
		}
	}

	public bool DataEnabled
	{
		set
		{
			Helper.Set(value, ref m_DataEnabled);
		}
	}

	public void Set(ref UpdateReceivingOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		ParticipantId = other.ParticipantId;
		DataEnabled = other.DataEnabled;
	}

	public void Set(ref UpdateReceivingOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
			RoomName = other.Value.RoomName;
			ParticipantId = other.Value.ParticipantId;
			DataEnabled = other.Value.DataEnabled;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_ParticipantId);
	}
}
