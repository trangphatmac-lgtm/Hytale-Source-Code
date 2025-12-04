using Coherent.UI.Binding;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Data.Items;

[CoherentType]
public class ClientItemStack
{
	[CoherentProperty("itemId")]
	public string Id;

	[CoherentProperty("quantity")]
	public int Quantity;

	[CoherentProperty("durability")]
	public double Durability;

	[CoherentProperty("maxDurability")]
	public double MaxDurability;

	public JObject Metadata;

	[CoherentProperty("metadata")]
	public readonly string StringifiedMetadata;

	public ClientItemStack()
	{
	}

	public ClientItemStack(string id, int quantity = 1)
	{
		Id = id;
		Quantity = quantity;
	}

	public ClientItemStack(Item packet)
	{
		Id = packet.ItemId;
		Quantity = packet.Quantity;
		Durability = packet.Durability;
		MaxDurability = packet.MaxDurability;
		if (packet.Metadata != null)
		{
			Metadata = ProtoHelper.DeserializeBson((byte[])(object)packet.Metadata);
			StringifiedMetadata = ((object)Metadata)?.ToString();
		}
	}

	public ClientItemStack(ClientItemStack other)
	{
		Id = other.Id;
		Quantity = other.Quantity;
		Durability = other.Durability;
		MaxDurability = other.MaxDurability;
		Metadata = other.Metadata;
		StringifiedMetadata = other.StringifiedMetadata;
	}

	public override string ToString()
	{
		return $"ItemId: {Id}, Quantity: {Quantity}, Durability: {Durability}, MaxDurability: {MaxDurability}, Metadata: {Metadata}";
	}

	public Item ToItemPacket(bool includeMetadata)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		return new Item(Id, Quantity, Durability, MaxDurability, false, includeMetadata ? ((sbyte[])(object)ProtoHelper.SerializeBson(Metadata)) : null);
	}

	public ClientItemStack Clone()
	{
		return new ClientItemStack(this);
	}

	public bool IsEquivalentType(ClientItemStack itemStack)
	{
		if (itemStack == null)
		{
			return false;
		}
		if (!Id.Equals(itemStack.Id))
		{
			return false;
		}
		return (Metadata != null) ? ((object)Metadata).Equals((object?)itemStack.Metadata) : (itemStack.Metadata == null);
	}

	public static bool IsEquivalent(ClientItemStack itemOne, ClientItemStack itemTwo)
	{
		return itemOne == itemTwo || (itemOne?.IsEquivalentType(itemTwo) ?? false);
	}
}
