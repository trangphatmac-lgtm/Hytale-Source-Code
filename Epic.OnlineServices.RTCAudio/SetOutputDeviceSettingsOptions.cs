namespace Epic.OnlineServices.RTCAudio;

public struct SetOutputDeviceSettingsOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RealDeviceId { get; set; }
}
