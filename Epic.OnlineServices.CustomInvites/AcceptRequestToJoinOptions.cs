namespace Epic.OnlineServices.CustomInvites;

public struct AcceptRequestToJoinOptions
{
	public ProductUserId LocalUserId { get; set; }

	public ProductUserId TargetUserId { get; set; }
}
