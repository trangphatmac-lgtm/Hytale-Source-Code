using System;

namespace Epic.OnlineServices.Leaderboards;

public struct Definition
{
	public Utf8String LeaderboardId { get; set; }

	public Utf8String StatName { get; set; }

	public LeaderboardAggregation Aggregation { get; set; }

	public DateTimeOffset? StartTime { get; set; }

	public DateTimeOffset? EndTime { get; set; }

	internal void Set(ref DefinitionInternal other)
	{
		LeaderboardId = other.LeaderboardId;
		StatName = other.StatName;
		Aggregation = other.Aggregation;
		StartTime = other.StartTime;
		EndTime = other.EndTime;
	}
}
