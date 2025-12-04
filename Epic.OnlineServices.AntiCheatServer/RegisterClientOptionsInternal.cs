using System;
using System.Runtime.InteropServices;
using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct RegisterClientOptionsInternal : ISettable<RegisterClientOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private AntiCheatCommonClientType m_ClientType;

	private AntiCheatCommonClientPlatform m_ClientPlatform;

	private IntPtr m_AccountId_DEPRECATED;

	private IntPtr m_IpAddress;

	private IntPtr m_UserId;

	private int m_Reserved01;

	public IntPtr ClientHandle
	{
		set
		{
			m_ClientHandle = value;
		}
	}

	public AntiCheatCommonClientType ClientType
	{
		set
		{
			m_ClientType = value;
		}
	}

	public AntiCheatCommonClientPlatform ClientPlatform
	{
		set
		{
			m_ClientPlatform = value;
		}
	}

	public Utf8String AccountId_DEPRECATED
	{
		set
		{
			Helper.Set(value, ref m_AccountId_DEPRECATED);
		}
	}

	public Utf8String IpAddress
	{
		set
		{
			Helper.Set(value, ref m_IpAddress);
		}
	}

	public ProductUserId UserId
	{
		set
		{
			Helper.Set(value, ref m_UserId);
		}
	}

	public int Reserved01
	{
		set
		{
			m_Reserved01 = value;
		}
	}

	public void Set(ref RegisterClientOptions other)
	{
		m_ApiVersion = 3;
		ClientHandle = other.ClientHandle;
		ClientType = other.ClientType;
		ClientPlatform = other.ClientPlatform;
		AccountId_DEPRECATED = other.AccountId_DEPRECATED;
		IpAddress = other.IpAddress;
		UserId = other.UserId;
		Reserved01 = other.Reserved01;
	}

	public void Set(ref RegisterClientOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 3;
			ClientHandle = other.Value.ClientHandle;
			ClientType = other.Value.ClientType;
			ClientPlatform = other.Value.ClientPlatform;
			AccountId_DEPRECATED = other.Value.AccountId_DEPRECATED;
			IpAddress = other.Value.IpAddress;
			UserId = other.Value.UserId;
			Reserved01 = other.Value.Reserved01;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientHandle);
		Helper.Dispose(ref m_AccountId_DEPRECATED);
		Helper.Dispose(ref m_IpAddress);
		Helper.Dispose(ref m_UserId);
	}
}
