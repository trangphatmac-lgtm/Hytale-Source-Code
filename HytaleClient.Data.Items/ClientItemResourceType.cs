using Coherent.UI.Binding;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

[CoherentType]
internal class ClientItemResourceType
{
	[CoherentProperty("id")]
	public readonly string Id;

	[CoherentProperty("quantity")]
	public readonly int Quantity;

	public ClientItemResourceType(ItemResourceType resource)
	{
		Id = resource.Id;
		Quantity = resource.Quantity;
	}
}
