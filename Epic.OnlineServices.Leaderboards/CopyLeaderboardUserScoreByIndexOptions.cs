namespace Epic.OnlineServices.Leaderboards;

public struct CopyLeaderboardUserScoreByIndexOptions
{
	public uint LeaderboardUserScoreIndex { get; set; }

	public Utf8String StatName { get; set; }
}
