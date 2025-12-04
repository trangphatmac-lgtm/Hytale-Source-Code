using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct JoinRTCRoomOptionsInternal : ISettable<JoinRTCRoomOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	private IntPtr m_LocalUserId;

	private IntPtr m_LocalRTCOptions;

	public Utf8String LobbyId
	{
		set
		{
			Helper.Set(value, ref m_LobbyId);
		}
	}

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public LocalRTCOptions? LocalRTCOptions
	{
		set
		{
			Helper.Set<LocalRTCOptions, LocalRTCOptionsInternal>(ref value, ref m_LocalRTCOptions);
		}
	}

	public void Set(ref JoinRTCRoomOptions other)
	{
		m_ApiVersion = 1;
		LobbyId = other.LobbyId;
		LocalUserId = other.LocalUserId;
		LocalRTCOptions = other.LocalRTCOptions;
	}

	public void Set(ref JoinRTCRoomOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LobbyId = other.Value.LobbyId;
			LocalUserId = other.Value.LocalUserId;
			LocalRTCOptions = other.Value.LocalRTCOptions;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_LocalRTCOptions);
	}
}
