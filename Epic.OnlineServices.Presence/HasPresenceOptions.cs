namespace Epic.OnlineServices.Presence;

public struct HasPresenceOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
