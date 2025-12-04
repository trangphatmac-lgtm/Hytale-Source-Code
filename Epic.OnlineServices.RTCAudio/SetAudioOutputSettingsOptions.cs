namespace Epic.OnlineServices.RTCAudio;

public struct SetAudioOutputSettingsOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String DeviceId { get; set; }

	public float Volume { get; set; }
}
