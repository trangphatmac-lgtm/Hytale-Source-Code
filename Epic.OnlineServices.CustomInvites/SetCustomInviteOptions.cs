namespace Epic.OnlineServices.CustomInvites;

public struct SetCustomInviteOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String Payload { get; set; }
}
