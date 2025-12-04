namespace Epic.OnlineServices.RTCData;

public struct UpdateSendingOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public bool DataEnabled { get; set; }
}
