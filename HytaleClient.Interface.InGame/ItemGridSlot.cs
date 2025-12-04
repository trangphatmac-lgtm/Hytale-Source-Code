using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame;

[UIMarkupData]
internal class ItemGridSlot
{
	public ClientItemStack ItemStack;

	public PatchStyle Background;

	public PatchStyle Overlay;

	public PatchStyle Icon;

	public bool IsItemIncompatible;

	public string Name;

	public string Description;

	public int? InventorySlotIndex;

	public bool SkipItemQualityBackground;

	public bool IsActivatable = true;

	public TextureArea ItemIcon { get; private set; }

	public TexturePatch BackgroundPatch { get; private set; }

	public TexturePatch OverlayPatch { get; private set; }

	public TextureArea IconTextureArea { get; private set; }

	public ItemGridSlot()
	{
	}

	public ItemGridSlot(ClientItemStack itemStack)
	{
		ItemStack = itemStack;
	}

	public void ApplyStyles(InGameView inGameView, Desktop desktop)
	{
		if (ItemStack != null && inGameView.Items.TryGetValue(ItemStack.Id, out var value))
		{
			ItemIcon = inGameView.GetTextureAreaForItemIcon(value.Icon);
		}
		else
		{
			ItemIcon = null;
		}
		BackgroundPatch = ((Background != null) ? desktop.MakeTexturePatch(Background) : null);
		OverlayPatch = ((Overlay != null) ? desktop.MakeTexturePatch(Overlay) : null);
		IconTextureArea = ((Icon != null) ? (Icon.TextureArea ?? ((Icon.TexturePath != null) ? desktop.Provider.MakeTextureArea(Icon.TexturePath.Value) : desktop.Provider.WhitePixel)) : null);
	}
}
