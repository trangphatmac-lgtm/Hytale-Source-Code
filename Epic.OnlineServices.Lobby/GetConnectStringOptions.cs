namespace Epic.OnlineServices.Lobby;

public struct GetConnectStringOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String LobbyId { get; set; }
}
