namespace Epic.OnlineServices.RTCAudio;

public struct SetAudioInputSettingsOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String DeviceId { get; set; }

	public float Volume { get; set; }

	public bool PlatformAEC { get; set; }
}
