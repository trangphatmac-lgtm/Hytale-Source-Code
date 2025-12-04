namespace Epic.OnlineServices.RTCAudio;

public struct SetInputDeviceSettingsOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RealDeviceId { get; set; }

	public bool PlatformAEC { get; set; }
}
