namespace Epic.OnlineServices.Presence;

public struct QueryPresenceOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public EpicAccountId TargetUserId { get; set; }
}
