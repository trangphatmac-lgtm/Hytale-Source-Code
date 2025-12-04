using System;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientUserWrapper
{
	public Guid Uuid;

	public string Name;

	public string AvatarUrl;

	public string CurrentState;

	public ClientUserWrapper(ClientUser user)
	{
		Uuid = user.Uuid_;
		Name = user.Name;
		AvatarUrl = user.AvatarUrl;
		CurrentState = user.CurrentState;
	}

	public ClientUserWrapper(Guid uuid, string name, string avatarUrl)
	{
		Uuid = uuid;
		Name = name;
		AvatarUrl = avatarUrl;
	}
}
