namespace Epic.OnlineServices.KWS;

public struct RequestPermissionsOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String[] PermissionKeys { get; set; }
}
