using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.IntegratedPlatform;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct OptionsInternal : IGettable<Options>, ISettable<Options>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Type;

	private IntegratedPlatformManagementFlags m_Flags;

	private IntPtr m_InitOptions;

	public Utf8String Type
	{
		get
		{
			Helper.Get(m_Type, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_Type);
		}
	}

	public IntegratedPlatformManagementFlags Flags
	{
		get
		{
			return m_Flags;
		}
		set
		{
			m_Flags = value;
		}
	}

	public IntPtr InitOptions
	{
		get
		{
			return m_InitOptions;
		}
		set
		{
			m_InitOptions = value;
		}
	}

	public void Set(ref Options other)
	{
		m_ApiVersion = 1;
		Type = other.Type;
		Flags = other.Flags;
		InitOptions = other.InitOptions;
	}

	public void Set(ref Options? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			Type = other.Value.Type;
			Flags = other.Value.Flags;
			InitOptions = other.Value.InitOptions;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Type);
		Helper.Dispose(ref m_InitOptions);
	}

	public void Get(out Options output)
	{
		output = default(Options);
		output.Set(ref this);
	}
}
