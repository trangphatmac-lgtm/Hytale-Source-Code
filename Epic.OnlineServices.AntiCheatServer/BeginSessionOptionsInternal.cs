using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.AntiCheatServer;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct BeginSessionOptionsInternal : ISettable<BeginSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_RegisterTimeoutSeconds;

	private IntPtr m_ServerName;

	private int m_EnableGameplayData;

	private IntPtr m_LocalUserId;

	public uint RegisterTimeoutSeconds
	{
		set
		{
			m_RegisterTimeoutSeconds = value;
		}
	}

	public Utf8String ServerName
	{
		set
		{
			Helper.Set(value, ref m_ServerName);
		}
	}

	public bool EnableGameplayData
	{
		set
		{
			Helper.Set(value, ref m_EnableGameplayData);
		}
	}

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public void Set(ref BeginSessionOptions other)
	{
		m_ApiVersion = 3;
		RegisterTimeoutSeconds = other.RegisterTimeoutSeconds;
		ServerName = other.ServerName;
		EnableGameplayData = other.EnableGameplayData;
		LocalUserId = other.LocalUserId;
	}

	public void Set(ref BeginSessionOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 3;
			RegisterTimeoutSeconds = other.Value.RegisterTimeoutSeconds;
			ServerName = other.Value.ServerName;
			EnableGameplayData = other.Value.EnableGameplayData;
			LocalUserId = other.Value.LocalUserId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ServerName);
		Helper.Dispose(ref m_LocalUserId);
	}
}
