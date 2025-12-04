using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Lobby;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct AttributeInternal : IGettable<Attribute>, ISettable<Attribute>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Data;

	private LobbyAttributeVisibility m_Visibility;

	public AttributeData? Data
	{
		get
		{
			Helper.Get<AttributeDataInternal, AttributeData>(m_Data, out AttributeData? to);
			return to;
		}
		set
		{
			Helper.Set<AttributeData, AttributeDataInternal>(ref value, ref m_Data);
		}
	}

	public LobbyAttributeVisibility Visibility
	{
		get
		{
			return m_Visibility;
		}
		set
		{
			m_Visibility = value;
		}
	}

	public void Set(ref Attribute other)
	{
		m_ApiVersion = 1;
		Data = other.Data;
		Visibility = other.Visibility;
	}

	public void Set(ref Attribute? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			Data = other.Value.Data;
			Visibility = other.Value.Visibility;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Data);
	}

	public void Get(out Attribute output)
	{
		output = default(Attribute);
		output.Set(ref this);
	}
}
