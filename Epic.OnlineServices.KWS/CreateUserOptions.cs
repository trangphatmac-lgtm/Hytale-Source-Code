namespace Epic.OnlineServices.KWS;

public struct CreateUserOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String DateOfBirth { get; set; }

	public Utf8String ParentEmail { get; set; }
}
