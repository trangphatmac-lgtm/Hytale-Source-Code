namespace Epic.OnlineServices.Sessions;

public struct SendInviteOptions
{
	public Utf8String SessionName { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public ProductUserId TargetUserId { get; set; }
}
