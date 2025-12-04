namespace Epic.OnlineServices.Achievements;

public struct CopyUnlockedAchievementByAchievementIdOptions
{
	public ProductUserId UserId { get; set; }

	public Utf8String AchievementId { get; set; }
}
