namespace Epic.OnlineServices.Lobby;

public struct IsRTCRoomConnectedOptions
{
	public Utf8String LobbyId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
