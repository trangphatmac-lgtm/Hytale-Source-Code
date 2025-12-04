namespace Epic.OnlineServices.RTCAudio;

public struct AddNotifyParticipantUpdatedOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
