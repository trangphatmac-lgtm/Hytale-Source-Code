namespace Epic.OnlineServices.RTCData;

public struct ParticipantUpdatedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public ProductUserId ParticipantId { get; set; }

	public RTCDataStatus DataStatus { get; set; }

	public Result? GetResultCode()
	{
		return null;
	}

	internal void Set(ref ParticipantUpdatedCallbackInfoInternal other)
	{
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		ParticipantId = other.ParticipantId;
		DataStatus = other.DataStatus;
	}
}
