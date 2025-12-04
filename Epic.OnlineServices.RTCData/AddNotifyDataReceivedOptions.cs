namespace Epic.OnlineServices.RTCData;

public struct AddNotifyDataReceivedOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
