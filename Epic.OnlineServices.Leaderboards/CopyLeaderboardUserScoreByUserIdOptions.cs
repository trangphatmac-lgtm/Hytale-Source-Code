namespace Epic.OnlineServices.Leaderboards;

public struct CopyLeaderboardUserScoreByUserIdOptions
{
	public ProductUserId UserId { get; set; }

	public Utf8String StatName { get; set; }
}
