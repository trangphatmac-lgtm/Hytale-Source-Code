using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct CredentialsInternal : IGettable<Credentials>, ISettable<Credentials>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Token;

	private ExternalCredentialType m_Type;

	public Utf8String Token
	{
		get
		{
			Helper.Get(m_Token, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_Token);
		}
	}

	public ExternalCredentialType Type
	{
		get
		{
			return m_Type;
		}
		set
		{
			m_Type = value;
		}
	}

	public void Set(ref Credentials other)
	{
		m_ApiVersion = 1;
		Token = other.Token;
		Type = other.Type;
	}

	public void Set(ref Credentials? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			Token = other.Value.Token;
			Type = other.Value.Type;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Token);
	}

	public void Get(out Credentials output)
	{
		output = default(Credentials);
		output.Set(ref this);
	}
}
