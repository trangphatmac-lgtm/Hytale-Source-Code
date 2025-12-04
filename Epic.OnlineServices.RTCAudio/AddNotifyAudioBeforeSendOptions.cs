namespace Epic.OnlineServices.RTCAudio;

public struct AddNotifyAudioBeforeSendOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
