namespace Epic.OnlineServices.Lobby;

public struct JoinRTCRoomOptions
{
	public Utf8String LobbyId { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public LocalRTCOptions? LocalRTCOptions { get; set; }
}
