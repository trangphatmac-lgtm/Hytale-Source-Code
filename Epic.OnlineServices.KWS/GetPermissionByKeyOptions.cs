namespace Epic.OnlineServices.KWS;

public struct GetPermissionByKeyOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String Key { get; set; }
}
