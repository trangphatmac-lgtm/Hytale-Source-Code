using System;

namespace Epic.OnlineServices.Leaderboards;

public struct QueryLeaderboardUserScoresOptions
{
	public ProductUserId[] UserIds { get; set; }

	public UserScoresQueryStatInfo[] StatInfo { get; set; }

	public DateTimeOffset? StartTime { get; set; }

	public DateTimeOffset? EndTime { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
