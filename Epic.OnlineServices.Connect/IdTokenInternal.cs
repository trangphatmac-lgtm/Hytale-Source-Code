using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct IdTokenInternal : IGettable<IdToken>, ISettable<IdToken>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ProductUserId;

	private IntPtr m_JsonWebToken;

	public ProductUserId ProductUserId
	{
		get
		{
			Helper.Get(m_ProductUserId, out ProductUserId to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ProductUserId);
		}
	}

	public Utf8String JsonWebToken
	{
		get
		{
			Helper.Get(m_JsonWebToken, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_JsonWebToken);
		}
	}

	public void Set(ref IdToken other)
	{
		m_ApiVersion = 1;
		ProductUserId = other.ProductUserId;
		JsonWebToken = other.JsonWebToken;
	}

	public void Set(ref IdToken? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			ProductUserId = other.Value.ProductUserId;
			JsonWebToken = other.Value.JsonWebToken;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ProductUserId);
		Helper.Dispose(ref m_JsonWebToken);
	}

	public void Get(out IdToken output)
	{
		output = default(IdToken);
		output.Set(ref this);
	}
}
