namespace Epic.OnlineServices.Achievements;

public struct QueryPlayerAchievementsOptions
{
	public ProductUserId TargetUserId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
