namespace Epic.OnlineServices.RTCData;

public struct UpdateReceivingOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public ProductUserId ParticipantId { get; set; }

	public bool DataEnabled { get; set; }
}
