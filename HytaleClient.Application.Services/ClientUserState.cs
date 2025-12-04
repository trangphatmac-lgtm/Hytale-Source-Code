using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientUserState
{
	public bool Online;

	public bool InParty;

	public ClientUserState(ClientPlayerState state)
	{
		Online = state.Online;
		InParty = state.InParty;
	}
}
