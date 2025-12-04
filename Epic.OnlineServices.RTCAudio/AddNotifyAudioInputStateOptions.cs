namespace Epic.OnlineServices.RTCAudio;

public struct AddNotifyAudioInputStateOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
