using Coherent.UI.Binding;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

[CoherentType]
internal class ClientItemArmor
{
	[CoherentProperty("armorSlot")]
	public readonly ItemArmorSlot ArmorSlot;

	[CoherentProperty("cosmeticsToHide")]
	public readonly Cosmetic[] CosmeticsToHide;

	public ClientItemArmor(ItemArmor armor)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (armor != null)
		{
			ArmorSlot = armor.ArmorSlot;
			CosmeticsToHide = armor.CosmeticsToHide;
		}
	}
}
