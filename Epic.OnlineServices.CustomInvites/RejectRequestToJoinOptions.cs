namespace Epic.OnlineServices.CustomInvites;

public struct RejectRequestToJoinOptions
{
	public ProductUserId LocalUserId { get; set; }

	public ProductUserId TargetUserId { get; set; }
}
