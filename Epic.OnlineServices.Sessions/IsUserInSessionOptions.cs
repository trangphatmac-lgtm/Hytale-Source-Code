namespace Epic.OnlineServices.Sessions;

public struct IsUserInSessionOptions
{
	public Utf8String SessionName { get; set; }

	public ProductUserId TargetUserId { get; set; }
}
