using System.Collections.Generic;
using System.Linq;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

internal class ClientItemReticleConfig
{
	public string Id;

	public string[] Base;

	public Dictionary<int, ClientItemReticle> ServerEvents;

	public Dictionary<ItemReticleClientEvent, ClientItemReticle> ClientEvents;

	public ClientItemReticleConfig()
	{
	}

	public ClientItemReticleConfig(ItemReticleConfig packet)
	{
		Id = packet.Id;
		Base = packet.Base;
		ServerEvents = ((packet.ServerEvents == null) ? new Dictionary<int, ClientItemReticle>() : packet.ServerEvents.ToDictionary((KeyValuePair<int, ItemReticle> kvp) => kvp.Key, (KeyValuePair<int, ItemReticle> kvp) => new ClientItemReticle(kvp.Value)));
		ClientEvents = ((packet.ClientEvents == null) ? new Dictionary<ItemReticleClientEvent, ClientItemReticle>() : packet.ClientEvents.ToDictionary((KeyValuePair<ItemReticleClientEvent, ItemReticle> kvp) => kvp.Key, (KeyValuePair<ItemReticleClientEvent, ItemReticle> kvp) => new ClientItemReticle(kvp.Value)));
	}

	public ClientItemReticleConfig Clone()
	{
		ClientItemReticleConfig clientItemReticleConfig = new ClientItemReticleConfig();
		clientItemReticleConfig.Id = Id;
		clientItemReticleConfig.Base = Base;
		clientItemReticleConfig.ServerEvents = ServerEvents;
		clientItemReticleConfig.ClientEvents = ClientEvents;
		return clientItemReticleConfig;
	}
}
