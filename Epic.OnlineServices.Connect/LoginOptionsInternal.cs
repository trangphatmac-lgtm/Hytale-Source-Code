using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct LoginOptionsInternal : ISettable<LoginOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Credentials;

	private IntPtr m_UserLoginInfo;

	public Credentials? Credentials
	{
		set
		{
			Helper.Set<Credentials, CredentialsInternal>(ref value, ref m_Credentials);
		}
	}

	public UserLoginInfo? UserLoginInfo
	{
		set
		{
			Helper.Set<UserLoginInfo, UserLoginInfoInternal>(ref value, ref m_UserLoginInfo);
		}
	}

	public void Set(ref LoginOptions other)
	{
		m_ApiVersion = 2;
		Credentials = other.Credentials;
		UserLoginInfo = other.UserLoginInfo;
	}

	public void Set(ref LoginOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 2;
			Credentials = other.Value.Credentials;
			UserLoginInfo = other.Value.UserLoginInfo;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Credentials);
		Helper.Dispose(ref m_UserLoginInfo);
	}
}
