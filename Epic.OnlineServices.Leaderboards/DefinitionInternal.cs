using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Leaderboards;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct DefinitionInternal : IGettable<Definition>, ISettable<Definition>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LeaderboardId;

	private IntPtr m_StatName;

	private LeaderboardAggregation m_Aggregation;

	private long m_StartTime;

	private long m_EndTime;

	public Utf8String LeaderboardId
	{
		get
		{
			Helper.Get(m_LeaderboardId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_LeaderboardId);
		}
	}

	public Utf8String StatName
	{
		get
		{
			Helper.Get(m_StatName, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_StatName);
		}
	}

	public LeaderboardAggregation Aggregation
	{
		get
		{
			return m_Aggregation;
		}
		set
		{
			m_Aggregation = value;
		}
	}

	public DateTimeOffset? StartTime
	{
		get
		{
			Helper.Get(m_StartTime, out var to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_StartTime);
		}
	}

	public DateTimeOffset? EndTime
	{
		get
		{
			Helper.Get(m_EndTime, out var to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_EndTime);
		}
	}

	public void Set(ref Definition other)
	{
		m_ApiVersion = 1;
		LeaderboardId = other.LeaderboardId;
		StatName = other.StatName;
		Aggregation = other.Aggregation;
		StartTime = other.StartTime;
		EndTime = other.EndTime;
	}

	public void Set(ref Definition? other)
	{
		if (other.HasValue)
		{
			m_ApiVersion = 1;
			LeaderboardId = other.Value.LeaderboardId;
			StatName = other.Value.StatName;
			Aggregation = other.Value.Aggregation;
			StartTime = other.Value.StartTime;
			EndTime = other.Value.EndTime;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LeaderboardId);
		Helper.Dispose(ref m_StatName);
	}

	public void Get(out Definition output)
	{
		output = default(Definition);
		output.Set(ref this);
	}
}
