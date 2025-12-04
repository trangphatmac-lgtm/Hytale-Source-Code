namespace Epic.OnlineServices.Presence;

public struct CopyPresenceOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
