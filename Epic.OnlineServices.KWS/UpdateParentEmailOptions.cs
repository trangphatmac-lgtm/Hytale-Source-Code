namespace Epic.OnlineServices.KWS;

public struct UpdateParentEmailOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String ParentEmail { get; set; }
}
