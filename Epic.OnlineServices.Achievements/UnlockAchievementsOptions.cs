namespace Epic.OnlineServices.Achievements;

public struct UnlockAchievementsOptions
{
	public ProductUserId UserId { get; set; }

	public Utf8String[] AchievementIds { get; set; }
}
