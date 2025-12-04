namespace Epic.OnlineServices.Lobby;

public struct CopyLobbyDetailsHandleOptions
{
	public Utf8String LobbyId { get; set; }

	public ProductUserId LocalUserId { get; set; }
}
