namespace Epic.OnlineServices.Achievements;

public struct QueryDefinitionsOptions
{
	public ProductUserId LocalUserId { get; set; }

	internal EpicAccountId EpicUserId_DEPRECATED { get; set; }

	internal Utf8String[] HiddenAchievementIds_DEPRECATED { get; set; }
}
