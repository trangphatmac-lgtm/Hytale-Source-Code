namespace Epic.OnlineServices.RTCData;

public struct UpdateSendingCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String RoomName { get; set; }

	public bool DataEnabled { get; set; }

	public Result? GetResultCode()
	{
		return ResultCode;
	}

	internal void Set(ref UpdateSendingCallbackInfoInternal other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LocalUserId = other.LocalUserId;
		RoomName = other.RoomName;
		DataEnabled = other.DataEnabled;
	}
}
