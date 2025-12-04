namespace Epic.OnlineServices.Lobby;

public struct UpdateLobbyModificationOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String LobbyId { get; set; }
}
