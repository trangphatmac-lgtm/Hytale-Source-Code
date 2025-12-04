namespace Epic.OnlineServices.RTC;

public struct AddNotifyParticipantStatusChangedOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
