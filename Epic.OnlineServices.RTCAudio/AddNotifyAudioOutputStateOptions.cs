namespace Epic.OnlineServices.RTCAudio;

public struct AddNotifyAudioOutputStateOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
