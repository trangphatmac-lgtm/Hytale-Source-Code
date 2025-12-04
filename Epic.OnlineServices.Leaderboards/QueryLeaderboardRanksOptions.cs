namespace Epic.OnlineServices.Leaderboards;

public struct QueryLeaderboardRanksOptions
{
	public Utf8String LeaderboardId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
