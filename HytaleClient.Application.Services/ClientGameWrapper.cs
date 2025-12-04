using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientGameWrapper
{
	public readonly string NameKey;

	public readonly string DefaultName;

	public readonly int PlayerMin;

	public readonly int PlayerMax;

	public readonly string JoinKey;

	public readonly string ImageUrl;

	public readonly bool Featured;

	public readonly bool Display;

	public ClientGameWrapper(ClientGame game)
	{
		NameKey = game.NameKey;
		DefaultName = game.DefaultName;
		PlayerMin = game.PlayerMin;
		PlayerMax = game.PlayerMax;
		JoinKey = game.JoinKey;
		ImageUrl = game.ImageUrl;
		Featured = game.Featured;
		Display = game.Display;
	}
}
