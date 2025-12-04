namespace Epic.OnlineServices.Sessions;

public struct RegisterPlayersOptions
{
	public Utf8String SessionName { get; set; }

	public ProductUserId[] PlayersToRegister { get; set; }
}
