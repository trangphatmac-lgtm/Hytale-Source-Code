namespace Epic.OnlineServices.Presence;

public struct SetPresenceOptions
{
	public EpicAccountId LocalUserId { get; set; }

	public PresenceModification PresenceModificationHandle { get; set; }
}
