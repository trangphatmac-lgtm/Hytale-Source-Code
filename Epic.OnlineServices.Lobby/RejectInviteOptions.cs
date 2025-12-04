namespace Epic.OnlineServices.Lobby;

public struct RejectInviteOptions
{
	public Utf8String InviteId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
