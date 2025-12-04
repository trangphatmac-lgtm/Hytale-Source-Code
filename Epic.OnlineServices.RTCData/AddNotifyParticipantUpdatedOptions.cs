namespace Epic.OnlineServices.RTCData;

public struct AddNotifyParticipantUpdatedOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
