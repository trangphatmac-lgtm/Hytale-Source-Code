namespace Epic.OnlineServices.RTCAdmin;

public struct KickOptions
{
	public Utf8String RoomName { get; set; }

	public ProductUserId TargetUserId { get; set; }
}
