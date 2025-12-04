namespace Epic.OnlineServices.Lobby;

public struct DestroyLobbyOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String LobbyId { get; set; }
}
