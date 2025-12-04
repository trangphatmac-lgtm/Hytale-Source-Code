using HytaleClient.Graphics;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

internal class ClientItemQuality
{
	public string ItemTooltipTexture;

	public string ItemTooltipArrowTexture;

	public string SlotTexture;

	public string SpecialSlotTexture;

	public UInt32Color TextColor;

	public string LocalizationKey;

	public bool VisibleQualityLabel;

	public bool RenderSpecialSlot;

	public string Id { get; private set; }

	public ClientItemQuality()
	{
	}

	public ClientItemQuality(ItemQuality itemQuality)
	{
		Id = itemQuality.Id;
		ItemTooltipTexture = itemQuality.ItemTooltipTexture ?? "";
		ItemTooltipArrowTexture = itemQuality.ItemTooltipArrowTexture ?? "";
		SlotTexture = itemQuality.SlotTexture ?? "";
		SpecialSlotTexture = itemQuality.SpecialSlotTexture ?? "";
		TextColor = ((itemQuality.TextColor != null) ? UInt32Color.FromRGBA((byte)itemQuality.TextColor.Red, (byte)itemQuality.TextColor.Green, (byte)itemQuality.TextColor.Blue, byte.MaxValue) : UInt32Color.FromHexString("#c9d2dd"));
		LocalizationKey = itemQuality.LocalizationKey ?? "Missing";
		VisibleQualityLabel = itemQuality.VisibleQualityLabel;
		RenderSpecialSlot = itemQuality.RenderSpecialSlot;
	}

	public ClientItemQuality Clone()
	{
		ClientItemQuality clientItemQuality = new ClientItemQuality();
		clientItemQuality.Id = Id;
		clientItemQuality.ItemTooltipTexture = ItemTooltipTexture;
		clientItemQuality.ItemTooltipArrowTexture = ItemTooltipArrowTexture;
		clientItemQuality.SlotTexture = SlotTexture;
		clientItemQuality.SpecialSlotTexture = SpecialSlotTexture;
		clientItemQuality.TextColor = TextColor;
		clientItemQuality.LocalizationKey = LocalizationKey;
		clientItemQuality.VisibleQualityLabel = VisibleQualityLabel;
		clientItemQuality.RenderSpecialSlot = RenderSpecialSlot;
		return clientItemQuality;
	}
}
