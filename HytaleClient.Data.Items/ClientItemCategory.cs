using System.Linq;
using Coherent.UI.Binding;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

[CoherentType]
internal class ClientItemCategory
{
	[CoherentProperty("id")]
	public readonly string Id;

	[CoherentProperty("icon")]
	public readonly string Icon;

	[CoherentProperty("order")]
	public readonly int Order;

	[CoherentProperty("infoDisplayMode")]
	public readonly ItemGridInfoDisplayMode InfoDisplayMode;

	[CoherentProperty("children")]
	public readonly ClientItemCategory[] Children;

	public ClientItemCategory(ItemCategory packet)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		Id = packet.Id;
		Icon = packet.Icon;
		Order = packet.Order;
		InfoDisplayMode = packet.InfoDisplayMode;
		if (packet.Children != null)
		{
			Children = (from category in packet.Children
				where category != null
				select new ClientItemCategory(category)).ToArray();
		}
	}
}
