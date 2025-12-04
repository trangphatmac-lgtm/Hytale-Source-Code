using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.RTCData;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct UpdateSendingOptionsInternal : ISettable<UpdateSendingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

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

	public bool DataEnabled
	{
		set
		{
			Helper.Set(value, ref m_DataEnabled);
		}
	}

	public void Set(ref UpdateSendingOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		DataEnabled = other.DataEnabled;
	}

	public void Set(ref UpdateSendingOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
			RoomName = other.Value.RoomName;
			DataEnabled = other.Value.DataEnabled;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
	}
}
