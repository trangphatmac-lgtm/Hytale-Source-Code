using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Ecom;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct QueryEntitlementsOptionsInternal : ISettable<QueryEntitlementsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementNames;

	private uint m_EntitlementNameCount;

	private int m_IncludeRedeemed;

	private IntPtr m_OverrideCatalogNamespace;

	public EpicAccountId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public Utf8String[] EntitlementNames
	{
		set
		{
			Helper.Set(value, ref m_EntitlementNames, out m_EntitlementNameCount);
		}
	}

	public bool IncludeRedeemed
	{
		set
		{
			Helper.Set(value, ref m_IncludeRedeemed);
		}
	}

	public Utf8String OverrideCatalogNamespace
	{
		set
		{
			Helper.Set(value, ref m_OverrideCatalogNamespace);
		}
	}

	public void Set(ref QueryEntitlementsOptions other)
	{
		m_ApiVersion = 3;
		LocalUserId = other.LocalUserId;
		EntitlementNames = other.EntitlementNames;
		IncludeRedeemed = other.IncludeRedeemed;
		OverrideCatalogNamespace = other.OverrideCatalogNamespace;
	}

	public void Set(ref QueryEntitlementsOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 3;
			LocalUserId = other.Value.LocalUserId;
			EntitlementNames = other.Value.EntitlementNames;
			IncludeRedeemed = other.Value.IncludeRedeemed;
			OverrideCatalogNamespace = other.Value.OverrideCatalogNamespace;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementNames);
		Helper.Dispose(ref m_OverrideCatalogNamespace);
	}
}
