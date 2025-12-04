using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.KWS;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct CreateUserOptionsInternal : ISettable<CreateUserOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_DateOfBirth;

	private IntPtr m_ParentEmail;

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public Utf8String DateOfBirth
	{
		set
		{
			Helper.Set(value, ref m_DateOfBirth);
		}
	}

	public Utf8String ParentEmail
	{
		set
		{
			Helper.Set(value, ref m_ParentEmail);
		}
	}

	public void Set(ref CreateUserOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
		DateOfBirth = other.DateOfBirth;
		ParentEmail = other.ParentEmail;
	}

	public void Set(ref CreateUserOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
			DateOfBirth = other.Value.DateOfBirth;
			ParentEmail = other.Value.ParentEmail;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_DateOfBirth);
		Helper.Dispose(ref m_ParentEmail);
	}
}
