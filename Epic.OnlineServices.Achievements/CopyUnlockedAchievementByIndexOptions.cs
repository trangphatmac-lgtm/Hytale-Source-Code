namespace Epic.OnlineServices.Achievements;

public struct CopyUnlockedAchievementByIndexOptions
{
	public ProductUserId UserId { get; set; }

	public uint AchievementIndex { get; set; }
}
