namespace Epic.OnlineServices.IntegratedPlatform;

public struct SetUserLoginStatusOptions
{
	public Utf8String PlatformType { get; set; }

	public Utf8String LocalPlatformUserId { get; set; }

	public LoginStatus CurrentLoginStatus { get; set; }
}
