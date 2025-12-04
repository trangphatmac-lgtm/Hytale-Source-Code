using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame;

internal class FloatingItemComponent : InterfaceComponent
{
	private readonly InGameView InGameView;

	public ItemGridSlot Slot;

	public bool ShowDropIcon;

	private Font _quantityFont;

	private TexturePatch _dropIconTexturePatch;

	private TexturePatch _unknownItemIconPatch;

	private Rectangle _dropIconRectangle;

	public FloatingItemComponent(InGameView inGameView, Element parent)
		: base(inGameView.Interface, parent)
	{
		InGameView = inGameView;
		Anchor = new Anchor
		{
			Width = InGameView.DefaultItemGridStyle.SlotIconSize,
			Height = InGameView.DefaultItemGridStyle.SlotIconSize
		};
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_dropIconTexturePatch = Desktop.MakeTexturePatch(new PatchStyle("InGame/Pages/Inventory/DropIcon.png"));
		_unknownItemIconPatch = Desktop.MakeTexturePatch(new PatchStyle("InGame/Pages/Inventory/UnknownItemIcon.png"));
		_quantityFont = Desktop.Provider.GetFontFamily("Default").RegularFont;
		Slot.ApplyStyles(InGameView, Desktop);
	}

	protected override void LayoutSelf()
	{
		int num = Desktop.ScaleRound(32f);
		_dropIconRectangle = new Rectangle(_rectangleAfterPadding.X + _rectangleAfterPadding.Width - num / 2, _rectangleAfterPadding.Y - num / 2, num, num);
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (Slot.ItemIcon != null)
		{
			Desktop.Batcher2D.RequestDrawTexture(Slot.ItemIcon.Texture, Slot.ItemIcon.Rectangle, _rectangleAfterPadding, UInt32Color.White);
		}
		else
		{
			Desktop.Batcher2D.RequestDrawPatch(_unknownItemIconPatch, _rectangleAfterPadding, Desktop.Scale);
		}
		if (ShowDropIcon)
		{
			Desktop.Batcher2D.RequestDrawPatch(_dropIconTexturePatch, _dropIconRectangle, Desktop.Scale);
		}
		if (Slot.ItemStack.Quantity > 1)
		{
			int num = Desktop.ScaleRound((InGameView.DefaultItemGridStyle.SlotSize - InGameView.DefaultItemGridStyle.SlotIconSize) / 2);
			int num2 = Desktop.ScaleRound(InGameView.DefaultItemGridStyle.SlotIconSize);
			int num3 = Desktop.ScaleRound(InGameView.DefaultItemGridStyle.SlotSize);
			string text = Slot.ItemStack.Quantity.ToString();
			int num4 = _rectangleAfterPadding.X + num2 - Desktop.ScaleRound(_quantityFont.CalculateTextWidth(text) * 16f / (float)_quantityFont.BaseSize);
			float y = (float)(_rectangleAfterPadding.Y + num3 - num) - 26f * Desktop.Scale;
			Desktop.Batcher2D.RequestDrawText(_quantityFont, 16f * Desktop.Scale, text, new Vector3(num4, y, 0f), UInt32Color.White);
		}
	}
}
