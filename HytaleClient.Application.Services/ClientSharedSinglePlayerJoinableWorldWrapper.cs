using System;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientSharedSinglePlayerJoinableWorldWrapper
{
	public readonly Guid WorldId;

	public readonly Guid Owner;

	public readonly bool Hidden = false;

	public readonly string Name;

	public ClientSharedSinglePlayerJoinableWorldWrapper(ClientSSPJoinableWorld world)
	{
		WorldId = world.WorldId;
		Owner = world.Owner;
		Name = world.Name;
	}
}
