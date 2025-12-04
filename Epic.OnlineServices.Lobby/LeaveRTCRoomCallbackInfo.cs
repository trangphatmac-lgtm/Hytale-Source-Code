namespace Epic.OnlineServices.Lobby;

public struct LeaveRTCRoomCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public Utf8String LobbyId { get; set; }

	public Result? GetResultCode()
	{
		return ResultCode;
	}

	internal void Set(ref LeaveRTCRoomCallbackInfoInternal other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		LobbyId = other.LobbyId;
	}
}
