namespace Epic.OnlineServices.CustomInvites;

public struct SendRequestToJoinOptions
{
	public ProductUserId LocalUserId { get; set; }

	public ProductUserId TargetUserId { get; set; }
}
