namespace Epic.OnlineServices.Presence;

public struct GetJoinInfoOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
