using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.TitleStorage;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct QueryFileListOptionsInternal : ISettable<QueryFileListOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ListOfTags;

	private uint m_ListOfTagsCount;

	public ProductUserId LocalUserId
	{
		set
		{
			Helper.Set(value, ref m_LocalUserId);
		}
	}

	public Utf8String[] ListOfTags
	{
		set
		{
			Helper.Set(value, ref m_ListOfTags, out m_ListOfTagsCount);
		}
	}

	public void Set(ref QueryFileListOptions other)
	{
		m_ApiVersion = 1;
		LocalUserId = other.LocalUserId;
		ListOfTags = other.ListOfTags;
	}

	public void Set(ref QueryFileListOptions? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LocalUserId = other.Value.LocalUserId;
			ListOfTags = other.Value.ListOfTags;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ListOfTags);
	}
}
