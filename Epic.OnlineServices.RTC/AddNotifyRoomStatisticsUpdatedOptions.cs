namespace Epic.OnlineServices.RTC;

public struct AddNotifyRoomStatisticsUpdatedOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }
}
