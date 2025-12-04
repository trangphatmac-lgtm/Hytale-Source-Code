using Coherent.UI.Binding;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

[CoherentType]
public class ClientResourceType
{
	[CoherentProperty("id")]
	public readonly string Id;

	[CoherentProperty("icon")]
	public readonly string Icon;

	public ClientResourceType(ResourceType packet)
	{
		Id = packet.Id;
		Icon = packet.Icon;
	}
}
