namespace Epic.OnlineServices.Lobby;

public struct LeaveRTCRoomOptions
{
	public Utf8String LobbyId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
